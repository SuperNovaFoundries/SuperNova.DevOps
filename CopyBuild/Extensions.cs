using Newtonsoft.Json;

namespace SuperNova.DevOps
{
    public static class Extensions
    {
        public static string ToJsonString(this object that, bool indented = false)
        {
            return JsonConvert.SerializeObject(that, indented ? Formatting.Indented : Formatting.None);
        }
    }
}
