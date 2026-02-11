using System.Reflection;

namespace ApiKeys
{
    public static class ApiKeyFolder
    {
        public static string? ReadKey(string location)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(nameof(location));

            if ( (location.Contains(Path.DirectorySeparatorChar) == false) && (location.Contains(Path.AltDirectorySeparatorChar)==false))
            {
                location = GetLocation() + Path.DirectorySeparatorChar + location;
            }
            if (File.Exists(location))
            {
                return File.ReadAllText(location);
            }
            else
            {
                if (File.Exists(location + ".KEY"))
                {
                    return File.ReadAllText(location + ".KEY");
                }
                else
                {
                    return null;
                }
            }
                
         
        }
        public static string GetLocation()
        {
            string? p = Assembly.GetCallingAssembly().Location;
            p = Path.GetDirectoryName(p);
            if (p == null)
            {
                throw new InvalidDataException("Assembly.GetCallingAssembly().Location unspecified null.");
            }
            p += Path.DirectorySeparatorChar + "keys";
            return p;
        }
    }
}
