using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System; // Necesar pentru StringComparison si Serializable

public class DynamicLandmarkHandler : MonoBehaviour // Am redenumit
{
    // --- Configurarea Maparii Landmark <-> Prefab ---
    [System.Serializable] // Permite ca structura sa apara in Inspector
    public struct LandmarkPrefabMapping
    {
        [Tooltip("Numele landmark-ului (partea din numele imaginii INAINTE de ultimul '_'). Ex: 'Cuiu', 'TurnulStefan'. Trebuie sa fie EXACT.")]
        public string landmarkKey;
        [Tooltip("Prefab-ul AR care va fi afisat pentru acest landmark.")]
        public GameObject prefab;
    }

    [Header("Mapare Landmark -> Prefab")]
    [Tooltip("Completeaza aceasta lista cu numele fiecarui landmark (cheia) si prefab-ul asociat.")]
    public List<LandmarkPrefabMapping> landmarkPrefabSetup = new List<LandmarkPrefabMapping>();

    // Dictionar pentru mapare rapida (cheie landmark -> prefab)
    private Dictionary<string, GameObject> landmarkPrefabMap = new Dictionary<string, GameObject>();

    // --- Restul variabilelor (ca inainte) ---
    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> instantiatedPrefabs = new Dictionary<string, GameObject>();
    private HashSet<string> activeLandmarksThisFrame = new HashSet<string>();


    void Awake()
    {
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        if (trackedImageManager == null) { /* ... eroare ... */ enabled = false; return; }

        // --- Populam dictionarul de mapare din lista configurata in Inspector ---
        foreach (var mapping in landmarkPrefabSetup)
        {
            if (!string.IsNullOrEmpty(mapping.landmarkKey) && mapping.prefab != null)
            {
                if (!landmarkPrefabMap.ContainsKey(mapping.landmarkKey))
                {
                    landmarkPrefabMap.Add(mapping.landmarkKey, mapping.prefab);
                    Debug.Log($"Mapare adaugata: Key='{mapping.landmarkKey}' -> Prefab='{mapping.prefab.name}'");
                }
                else
                {
                    Debug.LogWarning($"Cheia de landmark '{mapping.landmarkKey}' este duplicata in lista de mapare. Se ignora intrarea suplimentara.");
                }
            }
            else
            {
                Debug.LogWarning("Intrare invalida gasita in lista de mapare Landmark -> Prefab (cheie sau prefab lipsa).");
            }
        }
         Debug.Log($"Maparea Landmark -> Prefab finalizata. {landmarkPrefabMap.Count} intrari valide.");
    }

    // OnEnable, OnDisable - raman la fel ca in scriptul anterior

    void Update()
    {
        UpdatePrefabVisibility();
        activeLandmarksThisFrame.Clear();
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added) { ProcessTrackedImage(trackedImage); }
        foreach (ARTrackedImage trackedImage in eventArgs.updated) { ProcessTrackedImage(trackedImage); }
    }

    void ProcessTrackedImage(ARTrackedImage trackedImage)
    {
        string detectedImageName = trackedImage.referenceImage.name;
        string landmarkKey = ExtractLandmarkKey(detectedImageName);
        GameObject prefabToUse = null;

        // --- Gasim prefab-ul pe baza cheii extrase, folosind dictionarul ---
        if (landmarkKey != null && landmarkPrefabMap.TryGetValue(landmarkKey, out prefabToUse))
        {
            // Am gasit o mapare valida pentru cheia extrasa si avem un prefab asociat

            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                if (!instantiatedPrefabs.ContainsKey(landmarkKey))
                {
                    instantiatedPrefabs[landmarkKey] = Instantiate(prefabToUse, trackedImage.transform.position, trackedImage.transform.rotation);
                }
                instantiatedPrefabs[landmarkKey].transform.SetPositionAndRotation(trackedImage.transform.position, trackedImage.transform.rotation);
                activeLandmarksThisFrame.Add(landmarkKey);
            }
            // else { /* Handle non-tracking states if needed */ }
        }
        else if(landmarkKey != null)
        {
            // Am extras o cheie, dar nu exista in maparea noastra
            Debug.LogWarning($"Nu s-a gasit nicio mapare de prefab pentru cheia de landmark: '{landmarkKey}' (extrasa din imaginea '{detectedImageName}')");
        }
        // Daca landmarkKey e null, inseamna ca nu s-a putut extrage (eroare in ExtractLandmarkKey)
    }

    // --- Functie noua pentru extragerea cheii ---
    string ExtractLandmarkKey(string imageName)
    {
        if (string.IsNullOrEmpty(imageName)) return null;

        int lastUnderscoreIndex = imageName.LastIndexOf('_');

        // Verificam daca exista un underscore si daca nu este primul sau ultimul caracter
        if (lastUnderscoreIndex > 0 && lastUnderscoreIndex < imageName.Length - 1)
        {
            return imageName.Substring(0, lastUnderscoreIndex);
        }
        else
        {
            // Nu respecta conventia Name_Suffix. Putem returna numele intreg sau null/warning.
            // Alegem sa returnam numele intreg ca fallback, poate e definit asa in mapare.
             Debug.LogWarning($"Numele imaginii '{imageName}' nu pare sa respecte conventia 'NumeLandmark_Sufix'. Se foloseste numele intreg ca si cheie.");
            return imageName;
            // Sau, daca vrei sa ignori imaginile care nu respecta conventia:
            // Debug.LogWarning($"Numele imaginii '{imageName}' nu respecta conventia 'NumeLandmark_Sufix'. Se ignora.");
            // return null;
        }
    }

    // Functia UpdatePrefabVisibility - ramane identica cu cea din scriptul anterior
    // (Foloseste 'instantiatedPrefabs' si 'activeLandmarksThisFrame')
     void UpdatePrefabVisibility()
    {
        foreach (var pair in instantiatedPrefabs)
        {
            string landmarkKey = pair.Key;
            GameObject prefabInstance = pair.Value;

            if (prefabInstance != null)
            {
                bool isActive = activeLandmarksThisFrame.Contains(landmarkKey);
                if (prefabInstance.activeSelf != isActive)
                {
                     prefabInstance.SetActive(isActive);
                }
            }
        }
    }
}