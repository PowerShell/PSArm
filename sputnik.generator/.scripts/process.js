const { spawn } = require('child_process');
const { extname, isAbsolute, delimiter, resolve, dirname, basename } = require('path');
const fs = require('fs')
const { promisify } = require('util');
const lstat = promisify(fs.lstat);

async function isFile(path) {
  try {
    return (await lstat(path)).isFile();
  } catch (e) {
  }
  return false;
}
function cmdlineToArray(text, result = [], matcher = /[^\s"]+|"([^"]*)"/gi, count = 0) {
  text = text.replace(/\\"/g, '\ufffe');
  const match = matcher.exec(text);
  return match ? cmdlineToArray(text, result, matcher, result.push(match[1] ? match[1].replace(/\ufffe/g, '\\"') : match[0].replace(/\ufffe/g, '\\"'))) : result;
}
function quoteIfNecessary(text) {
  if (text && text.indexOf(' ') > -1 && text.charAt(0) != '"') {
    return `"${text}"`;
  }
  return text;
}
const nodePath = quoteIfNecessary(process.execPath);
function getPathVariableName() {
  // windows calls it's path 'Path' usually, but this is not guaranteed.
  if (process.platform === 'win32') {
    let PATH = 'Path';
    Object.keys(process.env).forEach(function (e) {
      if (e.match(/^PATH$/i)) {
        PATH = e;
      }
    });
    return PATH;
  }
  return 'PATH';
}
async function realPathWithExtension(command) {
  const pathExt = (process.env.pathext || '.EXE').split(';');
  for (const each of pathExt) {
    const filename = `${command}${each}`;
    if (await isFile(filename)) {
      return filename;
    }
  }
  return undefined;
}
async function getFullPath(command, searchPath) {
  command = command.replace(/"/g, '');
  const ext = extname(command);
  if (isAbsolute(command)) {
    // if the file has an extension, or we're not on win32, and this is an actual file, use it.
    if (ext || process.platform !== 'win32') {
      if (await isFile(command)) {
        return command;
      }
    }
    // if we're on windows, look for a file with an acceptable extension.
    if (process.platform === 'win32') {
      // try all the PATHEXT extensions to see if it is a recognized program
      const cmd = await realPathWithExtension(command);
      if (cmd) {
        return cmd;
      }
    }
    return undefined;
  }
  if (searchPath) {
    const folders = searchPath.split(delimiter);
    for (const each of folders) {
      const fullPath = await getFullPath(resolve(each, command));
      if (fullPath) {
        return fullPath;
      }
    }
  }
  return undefined;
}
/* 

interface MoreOptions extends SpawnOptions {
  onCreate?(cp: ChildProcess): void;
  onStdOutData?(chunk: any): void;
  onStdErrData?(chunk: any): void;
}
*/
const PathVar = getPathVariableName();

async function execute(cmd, cmdlineargs, options) {
  let command;
  if (!options && !Array.isArray(cmdlineargs)) {
    options = cmdlineargs;
  }
  options = options || {};
  if (Array.isArray(cmdlineargs)) {
    command = [cmd, ...cmdlineargs];
  } else {
    command = cmdlineToArray(cmd);
  }
  if (command[0] === 'node' || command[0] === 'node.exe') {
    command[0] = nodePath;
  }
  const env = { ...process.env };
  // ensure parameters requiring quotes have them.
  for (let i = 0; i < command.length; i++) {
    command[i] = quoteIfNecessary(command[i]);
  }
  const fullCommandPath = await getFullPath(command[0], env[getPathVariableName()]);

  // == special case ==
  // on Windows, if this command has a space in the name, and it's not an .EXE
  // then we're going to have to add the folder to the PATH
  // and execute it by just the filename
  // and set the path back when we're done.
  const special = process.platform === 'win32' && fullCommandPath.indexOf(' ') > -1 && !/.exe$/ig.exec(fullCommandPath);

  // preserve the current path
  const originalPath = process.env[PathVar];
  return new Promise((r, j) => {
    try {
      // insert the dir into the path
      if (special) {
        process.env[PathVar] = `${dirname(fullCommandPath)}${delimiter}${env[PathVar]}`;
      }
      // call spawn and return
      const cp = spawn(fullCommandPath, command.slice(1), { ...options, stdio: 'pipe' });

      cp.on('error', (err) => {
        console.log(`error! ${err} `);
      })

      if (options.onCreate) {
        options.onCreate(cp);
      }

      options.onStdOutData ? cp.stdout.on('data', options.onStdOutData) : cp;
      options.onStdErrData ? cp.stderr.on('data', options.onStdErrData) : cp;

      let err = '';
      let out = '';
      cp.stderr.on('data', (chunk) => {
        err += chunk;
      });
      cp.stdout.on('data', (chunk) => {
        out += chunk;
      });

      cp.on('close', (code, signal) => r({ stdout: out, stderr: err, error: code }));
    }
    finally {
      // regardless, restore the original path on the way out!
      process.env[PathVar] = originalPath;
    }
  });

}

function sleep(delayMS) {
  return new Promise(res => setTimeout(res, delayMS));
}

async function get(addr) {
  return new Promise((r, j) => require('http').request(addr, (response) => {
    response.on('error', (err) => {
      j(err);
    });

    if (response.statusCode === 200) {
      let data = "";
      response.on('data', (chunk) => {
        data = data + chunk.toString();
      });
      response.on('end', () => {
        r(data);
      });
      return;
    }
    j(response);
  }).end()
  )
}

module.exports = {
  execute,
  get,
  sleep
}