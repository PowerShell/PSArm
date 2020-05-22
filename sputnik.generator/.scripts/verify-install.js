try {
  console.log(`Verifying requirements to build.`);
  require('child_process').execSync('rush --help', { stdio: [] })

  console.log(`All requirements met.

  Common rush commands:
    # installs package dependencies  
    > rush update  

    # rebuilds all libraries
    > rush rebuild 

    # continual build when files change
    > rush watch   
`)

} catch {
  console.log(`
=== ERROR : REQUIRED INSTALL ===
  You must install 'rush' to continue:

  > npm install -g "@microsoft/rush"
  `)
  return 1;
} 