using System;
using System.Collections.Generic;
using System.Text;

namespace RobImpl
{
    public interface IJsonItemVisitor<T>
    {
        T VisitString(JsonString jStr);

        T VisitNumber(JsonNumber jNum);

        T VisitInteger(JsonInteger jInt);

        T VisitBoolean(JsonBoolean jBool);

        T VisitNull(JsonNull jNull);

        T VisitPointer(JsonPointer jPtr);

        T VisitObject(JsonObject jObj);

        T VisitArray(JsonArray jArr);
    }

    public enum JsonVisitAction
    {
        Continue,
        SkipChildren,
        Stop,
    }

    public class JsonItemVisitor : IJsonItemVisitor<JsonVisitAction>
    {
        public virtual JsonVisitAction VisitArray(JsonArray jArr)
        {
            return JsonVisitAction.Continue;
        }

        public virtual JsonVisitAction VisitBoolean(JsonBoolean jBool)
        {
            return JsonVisitAction.Continue;
        }

        public virtual JsonVisitAction VisitNull(JsonNull jNull)
        {
            return JsonVisitAction.Continue;
        }

        public virtual JsonVisitAction VisitNumber(JsonNumber jNum)
        {
            return JsonVisitAction.Continue;
        }

        public virtual JsonVisitAction VisitInteger(JsonInteger jInt)
        {
            return JsonVisitAction.Continue;
        }

        public virtual JsonVisitAction VisitObject(JsonObject jObj)
        {
            return JsonVisitAction.Continue;
        }

        public virtual JsonVisitAction VisitPointer(JsonPointer jPtr)
        {
            return JsonVisitAction.Continue;
        }

        public virtual JsonVisitAction VisitString(JsonString jStr)
        {
            return JsonVisitAction.Continue;
        }

        JsonVisitAction IJsonItemVisitor<JsonVisitAction>.VisitArray(JsonArray jArr)
        {
            JsonVisitAction action = VisitArray(jArr);

            switch (action)
            {
                case JsonVisitAction.Stop:
                    return JsonVisitAction.Stop;

                case JsonVisitAction.SkipChildren:
                    return JsonVisitAction.Continue;
            }

            for (int i = 0; action != JsonVisitAction.Stop && i < jArr.Items.Length; i++)
            {
                action = jArr.Items[i].Visit(this);

                if (action == JsonVisitAction.Stop)
                {
                    return JsonVisitAction.Stop;
                }
            }

            return JsonVisitAction.Continue;
        }

        JsonVisitAction IJsonItemVisitor<JsonVisitAction>.VisitBoolean(JsonBoolean jBool)
        {
            return VisitBoolean(jBool);
        }

        JsonVisitAction IJsonItemVisitor<JsonVisitAction>.VisitNull(JsonNull jNull)
        {
            return VisitNull(jNull);
        }

        JsonVisitAction IJsonItemVisitor<JsonVisitAction>.VisitNumber(JsonNumber jNum)
        {
            return VisitNumber(jNum);
        }

        JsonVisitAction IJsonItemVisitor<JsonVisitAction>.VisitInteger(JsonInteger jInt)
        {
            return VisitInteger(jInt);
        }

        JsonVisitAction IJsonItemVisitor<JsonVisitAction>.VisitObject(JsonObject jObj)
        {
            JsonVisitAction action = VisitObject(jObj);

            switch (action)
            {
                case JsonVisitAction.Stop:
                    return JsonVisitAction.Stop;

                case JsonVisitAction.SkipChildren:
                    return JsonVisitAction.Continue;
            }

            foreach (KeyValuePair<string, JsonItem> entry in jObj.Fields)
            {
                action = entry.Value.Visit(this);

                if (action == JsonVisitAction.Stop)
                {
                    return JsonVisitAction.Stop;
                }
            }

            return JsonVisitAction.Continue;
        }

        JsonVisitAction IJsonItemVisitor<JsonVisitAction>.VisitPointer(JsonPointer jPtr)
        {
            JsonVisitAction action = VisitPointer(jPtr);

            switch (action)
            {
                case JsonVisitAction.Stop:
                    return JsonVisitAction.Stop;

                case JsonVisitAction.SkipChildren:
                    return JsonVisitAction.Continue;
            }

            return jPtr.ResolvedItem.Visit(this);
        }

        JsonVisitAction IJsonItemVisitor<JsonVisitAction>.VisitString(JsonString jStr)
        {
            return VisitString(jStr);
        }
    }
}
