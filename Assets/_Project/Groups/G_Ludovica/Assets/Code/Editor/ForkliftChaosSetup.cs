#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace G_Ludovica.Editor
{
    /// <summary>Esegue in un colpo solo animazioni e sistemazione della scena dello scenario Forklift Chaos.</summary>
    public static class ForkliftChaosSetup
    {
        public static void Run()
        {
            ForkliftChaosAnimationBuilder.BuildAll();
            ForkliftChaosSceneFix.Run();
            AssetDatabase.SaveAssets();
            Debug.Log("[FKC-SETUP] Completato.");
        }
    }
}
#endif
