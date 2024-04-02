using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System.Collections.Generic;
using System.IO;

namespace DatasetGenerator.Server
{
    public class ServerMain : BaseScript
    {   
        

        public bool dirsMade = false;
        public ServerMain()
        {
            Debug.WriteLine("Hi from DatasetGenerator.Server!");

            EventHandlers["saveData"] += new Action<string, int, string>(SaveData);
            EventHandlers["generateDirs"] += new Action<string, string>(GenerateDirs);
            
        }

        private void SaveData(string saveDir, int id, string metadata)
        {   

            string metadataFile = $"{saveDir}\\labels\\{id}.txt";
 
            //Directory.CreateDirectory(metadataFile);
        
            File.WriteAllText(metadataFile, metadata);

            Debug.WriteLine($"{metadataFile} Saved");
        }

        private void GenerateDirs(string images_path,string labels_path){
                 if (!Directory.Exists(images_path)){
                    Directory.CreateDirectory(images_path);
                }

                if (!Directory.Exists(labels_path)){
                    Directory.CreateDirectory(labels_path);
                }
        }

    }
}
