



using System.Collections.Generic;

namespace Celeste.Mod.auspicioushelper;

public class auspicioushelperModuleSaveData : EverestModuleSaveData {
  
  public HashSet<string> collectedTrackedCassettes = new HashSet<string>();
  public Dictionary<string, int> savedataChannels = new();
}