using System.IO;
using System.Reflection;

namespace WpfApp1;

public class SettingsPersistenceLogic
{
    private string UserSettingsFilename;


    private string folderName;



    private string getFilePath(string? targetFolderName=null)
    {
        if (targetFolderName == null)
            targetFolderName = folderName;
        return Path.Join(targetFolderName, UserSettingsFilename);
    }

    public MySettings Settings { get; private set; }

    public SettingsPersistenceLogic(string userSettingsFilename)
    {
        this.UserSettingsFilename = userSettingsFilename;

        // if default settings exist
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        if (File.Exists(getFilePath(Path.Join(assemblyLocation, "Settings"))))
        {
            folderName = getFilePath(Path.Join(assemblyLocation, "Settings"));
            this.Settings = MySettings.Read(getFilePath());
        } else
        {
            folderName = Path.Join(assemblyLocation, "Settings", "UserSettings");
            if (File.Exists(getFilePath()))
                this.Settings = MySettings.Read(getFilePath());
            else
                this.Settings = new MySettings();
        }
    }


    public void SaveUserSettings()
    {
        Directory.CreateDirectory(folderName);
        Settings.Save(getFilePath());
    }
}