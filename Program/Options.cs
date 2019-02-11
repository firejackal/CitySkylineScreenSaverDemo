using System.Drawing;

public class OptionsManager
{
    private string RegistryPath = "SOFTWARE\\StrikeSoft\\" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

    public float MinScale = 0.3F;
    public float MaxScale = 0.8F;

    public Color NightColor   = Color.FromArgb(0, 0, 65);
    public Color SunriseColor = Color.FromArgb(255, 18, 0);
    public Color DayColor     = Color.FromArgb(153, 217, 234);
    public Color SunsetColor  = Color.FromArgb(221, 150, 114);

    public string DefaultLocation = "10001";
    public double DefaultLAT      = 40.7507985;
    public double DefaultLNG      = -73.9962255;

    public Color CloudColor = Color.White;
    public int CloudMinAlpha     = 150;
    public int CloudMaxAlpha     = 255;
    public int CloudLightCount   = 2;
    public int CloudPartialCount = 10;
    public int CloudMostlyCount  = 20;
    public int CloudFullCount    = 5;
    public float CloudMinWidth   = 0.4F;
    public float CloudMaxWidth   = 0.8F;
    public float CloudMinHeight  = 0.01F;
    public float CloudMaxHeight  = 0.1F;

    public string OverrideWeatherCondition = ""; //mostly_sunny, partly_cloudy, mostly_cloudy, mostly_cloudy, cloudy

    public void ReadSettings()
    {
        RegistryHelper reg = new RegistryHelper();
        this.DefaultLocation = (string)reg.GetValue(reg.HKeyLocalMachine, this.RegistryPath, "ZIP Code", "10001");
        this.OverrideWeatherCondition = (string)reg.GetValue(reg.HKeyLocalMachine, this.RegistryPath, "Override Weather Condition", "");
    } //ReadSettings

    public void SaveSettings()
    {
        RegistryHelper reg = new RegistryHelper();
        reg.SetValue(reg.HKeyLocalMachine, this.RegistryPath, "ZIP Code", this.DefaultLocation);
        reg.SetValue(reg.HKeyLocalMachine, this.RegistryPath, "Override Weather Condition", this.OverrideWeatherCondition);
    } //SaveSettings
} //OptionsManager
