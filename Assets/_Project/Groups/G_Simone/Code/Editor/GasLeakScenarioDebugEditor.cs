using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector di debug di MNG_Scenario. Con Debug Mode attivo mostra tutte le variabili
/// dei componenti di scenario e gameplay, inclusi i valori runtime in sola lettura.
/// </summary>
[CustomEditor(typeof(GasLeakScenario))]
public sealed class GasLeakScenarioDebugEditor : Editor
{
    const BindingFlags MEMBRI = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    readonly Dictionary<int, bool> _aperti = new Dictionary<int, bool>();
    SerializedProperty _debugMode;

    /// <summary>Prepara la proprietà del toggle di debug.</summary>
    void OnEnable()
    {
        _debugMode = serializedObject.FindProperty("debugMode");
    }

    /// <summary>Aggiorna i valori runtime mentre si è in Play Mode.</summary>
    public override bool RequiresConstantRepaint()
    {
        return _debugMode != null && _debugMode.boolValue;
    }

    /// <summary>Disegna l'Inspector normale e, se richiesto, il pannello di debug.</summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_debugMode != null)
        {
            EditorGUILayout.PropertyField(_debugMode);
        }

        DrawPropertiesExcluding(serializedObject, "debugMode");

        if (_debugMode != null && _debugMode.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Debug Mode: i valori qui sotto sono in sola lettura. Modifica i campi normali qui sopra.",
                MessageType.Info);
            DisegnaVariabili();
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>Mostra le variabili di tutti i componenti di MNG_Scenario.</summary>
    void DisegnaVariabili()
    {
        GasLeakScenario scenario = (GasLeakScenario)target;

        foreach (Component componente in scenario.gameObject.GetComponents<Component>())
        {
            if (componente == null || componente is Transform)
            {
                continue;
            }

            DisegnaComponente(componente);
        }
    }

    /// <summary>Mostra campi serializzati e stato runtime di un componente.</summary>
    void DisegnaComponente(Component componente)
    {
        Type tipo = componente.GetType();
        int chiave = componente.GetInstanceID();
        bool aperto = _aperti.TryGetValue(chiave, out bool salvato) ? salvato : true;
        aperto = EditorGUILayout.Foldout(aperto, tipo.Name, true);
        _aperti[chiave] = aperto;

        if (!aperto)
        {
            return;
        }

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Campi serializzati", EditorStyles.miniBoldLabel);
        DisegnaCampiSerializzati(componente);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Stato runtime", EditorStyles.miniBoldLabel);
        DisegnaStatoRuntime(componente, tipo);
        EditorGUI.indentLevel--;
    }

    /// <summary>Mostra i campi serializzati di un componente in sola lettura.</summary>
    void DisegnaCampiSerializzati(UnityEngine.Object oggetto)
    {
        SerializedObject serializzato = new SerializedObject(oggetto);
        SerializedProperty proprieta = serializzato.GetIterator();

        EditorGUI.BeginDisabledGroup(true);
        bool entraNeiFigli = true;
        while (proprieta.NextVisible(entraNeiFigli))
        {
            entraNeiFigli = false;
            if (proprieta.depth != 0 || proprieta.name == "m_Script")
            {
                continue;
            }

            EditorGUILayout.PropertyField(proprieta, true);
        }
        EditorGUI.EndDisabledGroup();
    }

    /// <summary>Mostra i campi non serializzati e le proprietà leggibili di un componente.</summary>
    void DisegnaStatoRuntime(Component componente, Type tipo)
    {
        IEnumerable<FieldInfo> campi = tipo.GetFields(MEMBRI)
            .Where(campo => !campo.IsStatic && !campo.IsLiteral && campo.GetCustomAttribute<SerializeField>() == null)
            .OrderBy(campo => campo.Name);

        foreach (FieldInfo campo in campi)
        {
            object valore = Leggi(() => campo.GetValue(componente));
            EditorGUILayout.LabelField(campo.Name, Formatta(valore));
        }

        IEnumerable<PropertyInfo> proprieta = tipo.GetProperties(MEMBRI)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && !EProprietaBase(property))
            .OrderBy(property => property.Name);

        foreach (PropertyInfo proprietaLetta in proprieta)
        {
            object valore = Leggi(() => proprietaLetta.GetValue(componente, null));
            EditorGUILayout.LabelField(proprietaLetta.Name, Formatta(valore));
        }
    }

    /// <summary>Esclude le proprietà generiche di Object, Component e MonoBehaviour.</summary>
    static bool EProprietaBase(PropertyInfo proprieta)
    {
        Type dichiarante = proprieta.DeclaringType;
        return dichiarante == typeof(UnityEngine.Object)
            || dichiarante == typeof(Component)
            || dichiarante == typeof(Behaviour)
            || dichiarante == typeof(MonoBehaviour);
    }

    /// <summary>Legge un valore senza far fallire l'Inspector se il getter genera errori.</summary>
    static object Leggi(Func<object> lettura)
    {
        try
        {
            return lettura();
        }
        catch (Exception)
        {
            return "<errore>";
        }
    }

    /// <summary>Formatta un valore per la riga dell'Inspector.</summary>
    static string Formatta(object valore)
    {
        if (valore == null)
        {
            return "null";
        }

        if (valore is string testo)
        {
            return testo;
        }

        if (valore is UnityEngine.Object oggetto)
        {
            return oggetto == null ? "null" : oggetto.name;
        }

        if (valore is VoceRecap voce)
        {
            return (voce.testo ?? string.Empty) + " → " + (voce.conseguenza ?? string.Empty);
        }

        if (valore is IEnumerable enumerabile)
        {
            int conteggio = 0;
            string primo = string.Empty;
            foreach (object elemento in enumerabile)
            {
                if (conteggio == 0)
                {
                    primo = Formatta(elemento);
                }

                conteggio++;
                if (conteggio >= 3)
                {
                    break;
                }
            }

            if (conteggio == 0)
            {
                return "vuoto";
            }

            return conteggio + (conteggio == 1 ? " elemento" : " elementi") + " [" + primo + "]";
        }

        if (valore is bool booleano)
        {
            return booleano ? "true" : "false";
        }

        if (valore is float numero)
        {
            return numero.ToString("0.00");
        }

        if (valore.GetType().IsSerializable)
        {
            try
            {
                string json = JsonUtility.ToJson(valore);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return json;
                }
            }
            catch (Exception)
            {
            }
        }

        return valore.ToString();
    }
}
