using SoulsFormats;
using System.Text;

namespace FlverMapMigrator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //CRITICAL, without this, shift jis handling will break and kill the application
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (args.Length > 1)
            {
                MigrateMap(args[0], args[1]);
            }
        }
        public static void MigrateMap(string oldMapDirectory, string newMapId)
        {
            string rootPath = Path.GetDirectoryName(oldMapDirectory);
            string newMapDirectory = Path.Combine(rootPath, newMapId + "_out");
            string oldMapId = Path.GetFileName(oldMapDirectory);
            string smallOldMapId = oldMapId.Substring(0, 3);
            string smallNewMapId = newMapId.Substring(0, 3);

            var files = Directory.GetFiles(oldMapDirectory);

            Directory.CreateDirectory(newMapDirectory);
            foreach (var file in files)
            {
                byte[] newFile = File.ReadAllBytes(file);
                DCX.Type dcxType = DCX.Type.None;
                if (SoulsFormats.DCX.Is(newFile))
                {
                    newFile = DCX.Decompress(newFile, out dcxType);
                }

                SoulsFile<BND4> bnd = null;
                if (SoulsFile<SoulsFormats.BND4>.Is(newFile))
                {
                    bnd = SoulsFile<SoulsFormats.BND4>.Read(newFile);
                }
                else
                {
                    continue;
                }

                bool shouldCopy = false;
                foreach (var bndFile in ((BND4)bnd).Files)
                {
                    var name = bndFile.Name;
                    var fileName = Path.GetFileName(name);
                    if (fileName.EndsWith(".flver"))
                    {
                        shouldCopy = true;
                        var flver = SoulsFormats.SoulsFile<SoulsFormats.FLVER2>.Read(bndFile.Bytes);
                        for (int i = 0; i < flver.Materials.Count; i++)
                        {
                            var material = flver.Materials[i];
                            for (int j = 0; j < material.Textures.Count; j++)
                            {
                                var texture = material.Textures[j];
                                texture.Path = texture.Path.Replace($"{oldMapId}\\tex\\", $"{newMapId}\\tex\\");
                                texture.Path = texture.Path.Replace($"{smallOldMapId}\\tex\\", $"{smallNewMapId}\\tex\\");
                            }
                        }
                        bndFile.Bytes = flver.Write();
                        bndFile.Name = bndFile.Name.Replace($"{oldMapId}", $"{newMapId}");
                        bndFile.Name = bndFile.Name.Replace($"{smallOldMapId}", $"{smallNewMapId}");
                    }
                }
                if (shouldCopy)
                {
                    string finalName = Path.GetFileName(file).Replace($"{oldMapId}", $"{newMapId}").Replace($"{smallOldMapId}", $"{smallNewMapId}");
                    File.WriteAllBytes(Path.Combine(newMapDirectory, finalName), bnd.Write(dcxType));
                }
            }
        }



    }
}