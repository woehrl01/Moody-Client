using System.IO;
using Android.Util;

namespace Moody
{
    public class SaveAndLoad{
        public void SaveText (string filename, string text) {
            var documentsPath = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
            var filePath = Path.Combine (documentsPath, filename);
            System.IO.File.WriteAllText (filePath, text);
            Log.Info("Save", "Succesful");
        }
        public string LoadText (string filename){
            var documentsPath = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
            var filePath = Path.Combine (documentsPath, filename);  
            return System.IO.File.ReadAllText (filePath);
        }
    }
}