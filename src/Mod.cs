﻿using System.Reflection;
namespace RegionKit;
/// <summary>
/// Main plugin class
/// </summary>
[BIE.BepInPlugin("rwmodding.coreorg.rk", "RegionKit", "3.8")]
public class Mod : BIE.BaseUnityPlugin
{
	internal const string RK_POM_CATEGORY = "RegionKit";
	private static Mod __inst = null!;
	//private readonly List<ActionWithData> _enableDels = new();
	private readonly List<ModuleInfo> _modules = new();
	private bool _modulesSetUp = false;
	private RainWorld _rw = null!;
	internal static LOG.ManualLogSource __logger => __inst.Logger;
	internal static RainWorld __RW => __inst._rw;
	///<inheritdoc/>
	public void OnEnable()
	{
		__inst = this;
		On.RainWorld.OnModsInit += Init;
		//Init();
		TheRitual.Commence();
	}

	private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		try { orig(self); }
		catch (Exception ex) { __logger.LogFatal($"Caught error in init-orig: {ex}"); }
		try
		{

			if (!_modulesSetUp)
			{
				ScanAssemblyForModules(typeof(Mod).Assembly);
				foreach (var mod in _modules)
				{
					RunEnableOn(mod);
				}
			}
			_modulesSetUp = true;
			_Assets.LoadResources();
		}
		catch (Exception ex)
		{
			__logger.LogError($"Error on init: {ex}");
		}
	}

	private void RunEnableOn(ModuleInfo mod)
	{
		try
		{
			mod.enable();
		}
		catch (Exception ex)
		{
			Logger.LogError($"Could not enable {mod.name}: {ex}");
		}
	}
	///<inheritdoc/>
	public void OnDisable()
	{
		On.RainWorld.OnModsInit -= Init;
		foreach (var mod in _modules)
		{
			RunDisableOn(mod);
		}
		__inst = null!;
	}

	private void RunDisableOn(ModuleInfo mod)
	{
		try
		{
			mod.disable();
		}
		catch (Exception ex)
		{
			Logger.LogError($"Error disabling {mod.name}: {ex}");
		}
	}
	///<inheritdoc/>
	public void FixedUpdate()
	{
		_rw ??= FindObjectOfType<RainWorld>();
		foreach (var mod in _modules)
		{
			try
			{
				mod.counter--;
				if (mod.counter < 1)
				{
					mod.counter = mod.period;
					mod?.tick?.Invoke();
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Module {mod.name} error in tick: {ex}");
			}
		}
	}
	#region module shenanigans
	///<inheritdoc/>
	public void ScanAssemblyForModules(RFL.Assembly asm)
	{
		Type[] types;
		try
		{
			types = asm.GetTypes();
		}
		catch (ReflectionTypeLoadException e)
		{
			types = e.Types.Where(t => t != null).ToArray();
			__logger.LogError(e);
			__logger.LogError(e.InnerException);
		}
		foreach (Type t in types)
		{
			if (t.IsGenericTypeDefinition) continue;
			TryRegisterModule(t);
		}
	}

	///<inheritdoc/>
	public void TryRegisterModule(Type? t)
	{
		if (t is null) return;
		foreach (RegionKitModuleAttribute moduleAttr in t.GetCustomAttributes(typeof(RegionKitModuleAttribute), false))
		{
			RFL.MethodInfo
				enable = t.GetMethod(moduleAttr._enableMethod, BF_ALL_CONTEXTS_STATIC),
				disable = t.GetMethod(moduleAttr._disableMethod, BF_ALL_CONTEXTS_STATIC);
			RFL.MethodInfo? tick = moduleAttr._tickMethod is string ticm ? t.GetMethod(ticm, BF_ALL_CONTEXTS_STATIC) : null;
			RFL.FieldInfo? loggerField = moduleAttr._loggerField is string logf ? t.GetField(logf, BF_ALL_CONTEXTS_STATIC) : null;
			string moduleName = moduleAttr._moduleName ?? t.FullName;
			if (enable is null || disable is null)
			{
				Logger.LogError($"Cannot register RegionKit module {t.FullName}: method contract incomplete ({moduleAttr._enableMethod} -> {enable}, {moduleAttr._disableMethod} -> {disable})");
				break;
			}
			Logger.LogMessage($"Registering module {moduleName}");
			if (loggerField is not null)
				try
				{
					loggerField.SetValue(null, BepInEx.Logging.Logger.CreateLogSource($"RegionKit/{moduleName}"), BF_ALL_CONTEXTS_STATIC, null, System.Globalization.CultureInfo.InvariantCulture
					);
				}
				catch (Exception ex) { Logger.LogWarning($"Invalid logger field name supplied! {ex}"); }
			Action
				enableDel = (Action)Delegate.CreateDelegate(typeof(Action), enable),
				disableDel = (Action)Delegate.CreateDelegate(typeof(Action), disable);
			Action? tickDel = tick is RFL.MethodInfo ntick ? (Action)Delegate.CreateDelegate(typeof(Action), ntick) : null;

			_modules.Add(new(moduleAttr._moduleName ?? t.FullName, enableDel, disableDel, tickDel, moduleAttr._tickPeriod)
			{
				counter = 0,
				errored = false
			});
			if (_modulesSetUp)
			{
				RunEnableOn(_modules.Last());
			}
			break;

		}
		foreach (Type nested in t.GetNestedTypes())
		{
			TryRegisterModule(nested);
		}
	}


	internal record ModuleInfo(
		string name,
		Action enable,
		Action disable,
		Action? tick,
		int period)
	{
		internal bool errored;
		internal int counter;
	};
	#endregion
}
