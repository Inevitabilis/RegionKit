﻿using DevInterface;
using System.IO;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.DevUIMisc;

internal class SettingsSaveOptions
{
	public static void SaveSignal(Page self, DevUISignalType type, DevUINode sender, string message)
	{
		if (settingsSaveOptionsMenu is null)
		{ return; }

		if (sender.IDstring == "Change_Path")
		{
			//remove panel with no changes when button clicked
			if (settingsSaveOptionsMenu.modSelectPanel != null)
			{
				self.subNodes.Remove(settingsSaveOptionsMenu.modSelectPanel);
				settingsSaveOptionsMenu.modSelectPanel.ClearSprites();
				settingsSaveOptionsMenu.modSelectPanel = null;
				return;
			}

			//create new panel when button clicked
			settingsSaveOptionsMenu.modSelectPanel = new ItemSelectPanel(self.owner, self, new Vector2(420f, 250f), settingsSaveOptionsMenu.modNames.Keys.ToArray(), "ModPanel", "Select Mod");
			self.subNodes.Add(settingsSaveOptionsMenu.modSelectPanel);
			return;
		}

		else if (settingsSaveOptionsMenu.modSelectPanel != null && sender.IDstring.StartsWith(settingsSaveOptionsMenu.modSelectPanel.idstring + "Button99289_"))
		{
			string subbuttonid = sender.IDstring.Remove(0, (settingsSaveOptionsMenu.modSelectPanel.idstring + "Button99289_").Length);

			if (settingsSaveOptionsMenu.ChangePath != null)
			{ settingsSaveOptionsMenu.ChangePath.Text = subbuttonid; }

			if (DevUIUtils.URoomSettings.PathToSpecificSettings(settingsSaveOptionsMenu.modNames[subbuttonid], self.RoomSettings.name, out string filePath, slugName: DevUIUtils.URoomSettings.UsingSpecificSlugcatName(self.owner)))
			{
				self.RoomSettings.filePath = filePath;
				Debug.Log($"new filepath is [{filePath}]");
				Debug.Log(subbuttonid);
				Debug.Log(settingsSaveOptionsMenu.modNames[subbuttonid]);
				settingsSaveOptionsMenu.RefreshPathLabel();
			}

			if (settingsSaveOptionsMenu.modSelectPanel != null)
			{
				self.subNodes.Remove(settingsSaveOptionsMenu.modSelectPanel);
				settingsSaveOptionsMenu.modSelectPanel.ClearSprites();
				settingsSaveOptionsMenu.modSelectPanel = null;
			}
		}

		else if (sender.IDstring == "Create_Modify" && settingsSaveOptionsMenu.ChangePath != null)
		{
			new ModifySettingsGenerator(self.owner, settingsSaveOptionsMenu.ChangePath.Text);
		}
	}


	public static SettingsSaveOptionsMenu? settingsSaveOptionsMenu;


	public class SettingsSaveOptionsMenu : DevUINode
	{
		public SettingsSaveOptionsMenu(DevUI owner, string IDstring, DevUINode parentNode) : base(owner, IDstring, parentNode)
		{

			modNames = new Dictionary<string, string>() {
				{"Default", DevUIUtils.URoomSettings.DefaultSettingsLocation(owner, RoomSettings, false)},
				{"vanilla", RootFolderDirectory()},
				{"mergedmods",Path.Combine(RootFolderDirectory(), "mergedmods") }
			};

			foreach (ModManager.Mod mod in ModManager.ActiveMods)
			{
				modNames.Add(mod.name, mod.path);
			}

			RefreshPathLabel();

			//SavedPath = new DevUILabel(owner, "Saved_Path", null, new Vector2(900f, 700f), 100f, "Default");

			//self.subNodes.Add(SavedPath);

			ChangePath = new Button(owner, "Change_Path", this, new Vector2(790f, 700f), 100f, "Default");

			subNodes.Add(ChangePath);


			CreateModify = new Button(owner, "Create_Modify", this, new Vector2(900f, 700f), 100f, "Create Modify");

			subNodes.Add(CreateModify);
		}

		public void RefreshPathLabel()
		{
			if (SettingsPathLabel != null && subNodes.Contains(SettingsPathLabel))
			{
				subNodes.Remove(SettingsPathLabel);
				SettingsPathLabel.ClearSprites();
				SettingsPathLabel = null;
				Debug.Log("removed settingspath");
			}

			string settingsPath = "Settings Path: " + RoomSettings.filePath.ToLower();

			DevUIUtils.UPath.TryCropToSubstringRight(settingsPath, "workshop", out settingsPath);

			DevUIUtils.UPath.TryCropToSubstringRight(settingsPath, "streamingassets", out settingsPath);

			Debug.Log("adding settingspath\n" + settingsPath);
			SettingsPathLabel = new DevUILabel(owner, "Settings_Path", this, new Vector2(1330f - 6f * settingsPath.Length, 20f), 20f + 6f * settingsPath.Length, settingsPath);

			subNodes.Add(SettingsPathLabel);
		}

		public ItemSelectPanel? modSelectPanel;

		public DevUILabel? SettingsPathLabel = null;

		public Button ChangePath;

		public Button CreateModify;

		public Dictionary<string, string> modNames;

	}
}
