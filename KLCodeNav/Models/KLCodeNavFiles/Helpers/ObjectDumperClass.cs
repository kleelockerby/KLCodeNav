using Newtonsoft.Json;

namespace KLCodeNav
{
    public static class Json
    {
        public static string DumpNewtonSoft(object obj)
        {
            //Console.WriteLine($"DumpNewtonSoft {Json.DumpNewtonSoft(j)} " + Environment.NewLine + Environment.NewLine);
            object objDump = obj ?? "Object is null";
            return JsonConvert.SerializeObject(objDump, Formatting.Indented);
        }

    }
}
