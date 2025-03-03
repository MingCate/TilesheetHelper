using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicData;
using System.Drawing;
using System.ComponentModel;
using Avalonia.Media;
using Color = System.Drawing.Color;
using System.Diagnostics;
using Avalonia;
using Avalonia.Input;
//using Newtonsoft.Json;

namespace TilesheetHelper
{
    public static class Configuration
    {
        public static readonly string SAVE_PATH = Path.Join(Directory.GetCurrentDirectory(), "settings.json");

        public static Dictionary<string, SettingBase> SettingsList = new();

        public static List<SettingGroup> settingGroups = new();
        public static SettingGroup outlineSettings = new ("Internal Outlines");


        //public static Setting<bool> fakeOption = new("Fake Setting", true);
        //public static Setting<Color> defaultColor = new("Default Color", Color.Red);
        //public static Setting<int> defaultNthDarkestColor = new("Use Nth Darkest Color for Internal Outlines", 2);
        public static Setting<bool> onlyReplaceLighterPixels = new(outlineSettings, "Only Replace Lighter Pixels", true); //needs a secret third option: automatically find darker color
        public static Setting<bool> alwaysExportDouble = new(null, "Always Export 2x Resolution", false); //TPDO: Make enum cycke?

        public static void SaveSettings()
        {
            List<string> json = new();
            foreach (var setting in SettingsList.Values)
            {
                json.Add(setting.JsonSerialize());
            }
            File.WriteAllLines(SAVE_PATH, json);
        }
        public static void LoadSettings()
        {
            if (!File.Exists(SAVE_PATH)) return;

            List<string> json = File.ReadAllLines(SAVE_PATH).ToList();
            List<string> errors = new();
            foreach (var settingString in json)
            {
                string name;
                string value = JsonDeserializeValue(settingString, out name);

                try
                {
                    SettingBase setting = SettingsList[name];
                    TypeConverter converter = TypeDescriptor.GetConverter(setting.Type);
                    setting.Value = converter.ConvertFromString(value);
                }
                catch (KeyNotFoundException ex)
                {
                    errors.Add($"No setting named \"{name}\" exists.");
                }
                catch (ArgumentException ex)
                {
                    errors.Add($"The setting \"{name}\" can't be set to \"{value}\"");
                }
            }
            //App.GetMainWindow().ShowAlert(TimeSpan.FromSeconds(10), App.MakeTextBlock("Couldn't parse settings.json:\n" + string.Join("\n", errors)));
            //Debug.WriteLine("Couldn't parse settings.json:\n" + string.Join("\n", errors));
            if (errors.Any())
            {
                errors.Add("Invalid settings are safely set to their default values and loaded normally.");
                App.ShowError("Some settings were set incorrectly", string.Join("\n", errors));
            }
        }
        public static string JsonDeserializeValue(string json, out string name)
        {
            name = json.SubstringBetween("\"", "\"");
            return json.SubstringBetween(": ", ",");
        }
    }
    public class Setting<T> : SettingBase
    {
        private T value;
        public override Border Control => CreateControl();
        private bool defaultValue;

        public override string Name { get; set; }
        public override Type Type { get; set; }
        private T DefaultValue { get; }
        public override object Value //force anything setting this to match the setting<T>'s type
        {
            get => value;
            set
            {
                this.value = (T)value;
            }
        }

        public Setting(SettingGroup? group, string name, T defaultValue)
        {
            Group = group;
            Name = name;
            DefaultValue = defaultValue;
            Value = defaultValue;
            Type = typeof(T);
            Configuration.SettingsList.Add(name, this);
        }

        public override string JsonSerialize()
        {
            TypeConverter converter = TypeDescriptor.GetConverter(Type);
            string? value = converter.ConvertToInvariantString(Value);

            return $"\"{Name}\": {value},";
        }
        private Border CreateControl()
        {
            Border control = new Border
            {
                Padding = Thickness.Parse("10, 8, 10, 8"),
                Background = App.GetColorBrush("BackgroundLight"),
                CornerRadius = new CornerRadius(10),
            };
            control.PointerPressed += SettingClicked;
            var panel = new DockPanel();
            control.Child = panel;

            var label = App.MakeTextBlock($"{Name}");
            label.Margin = Thickness.Parse("0, 0, 10, 0");
            DockPanel.SetDock(label, Dock.Left);
            panel.Children.Add(label);

            var button = RepresentValueAsButton();
            DockPanel.SetDock((Control)button, Dock.Right);
            panel.Children.Add(button);
            return control;
        }

        private void SettingClicked(object sender, EventArgs e)
        {
            Configuration.SaveSettings();
        }
        private IControl RepresentValueAsButton()
        {
            //IBrush textColor = App.GetColorBrush("TextLight");
            //IControl button = null;

            //if (this is Setting<bool> toggle) return new Toggle(toggle);

            switch (this)
            {
                case Setting<bool> toggle:
                    return new Toggle(toggle);
            }

            return new TextBlock { Text = $"{Value}", Foreground = App.GetColorBrush("TextLight") };
        }
    }
    public abstract class SettingBase
    {
        public abstract string Name { get; set; }
        public abstract Border Control { get; }
        public abstract object Value { get; set; }
        public abstract Type Type { get; set; }

        public SettingGroup? Group { get; set; }
        public abstract string JsonSerialize();

    }
    public class SettingGroup
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public List<SettingBase> Settings { get; set; }
        public SettingGroup(string name)
        {
            Name = name;
            Index = Configuration.settingGroups.Count;
            Settings = new ();
            Configuration.settingGroups.Add(this);
        }
    }
    //    public class SettingsManager
    //{
    //    public abstract class settingbase
    //    {
    //        public string name { get; }
    //        public abstract object value { get; set; }

    //        protected settingbase(string name)
    //        {
    //            name = name;
    //        }

    //        public abstract void savesettings();
    //    }

    //    private list<settingbase> settings = new list<settingbase>();

    //    public void addsetting<t>(setting<t> setting)
    //    {
    //        settings.add(setting);
    //    }

    //    public void savesettings()
    //    {
    //        foreach (var setting in settings)
    //        {
    //            setting.savesettings();
    //        }
    //    }
    //}
}
