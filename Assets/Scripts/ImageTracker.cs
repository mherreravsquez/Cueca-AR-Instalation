using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Video;

public class ImageTracker : MonoBehaviour
{
    [System.Serializable]
    public struct ImageStandPair
    {
        public string imageName;
        public GameObject standPrefab;
    }

    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private List<ImageStandPair> imageStandPairs;

    private Dictionary<string, GameObject> instantiatedStands = new Dictionary<string, GameObject>();
    private Dictionary<string, VideoPlayer> videoPlayers = new Dictionary<string, VideoPlayer>();
    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();
    private Dictionary<string, bool> standReadyStates = new Dictionary<string, bool>();

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            HandleTrackedImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            HandleTrackedImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            DeactivateStand(imageName);
        }
    }

    private void HandleTrackedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        
        if (!instantiatedStands.ContainsKey(imageName))
        {
            CreateStandForImage(trackedImage, imageName);
        }

        UpdateStandState(trackedImage, imageName);
    }

    private void CreateStandForImage(ARTrackedImage trackedImage, string imageName)
    {
        GameObject standPrefab = null;
        foreach (var pair in imageStandPairs)
        {
            if (pair.imageName == imageName)
            {
                standPrefab = pair.standPrefab;
                break;
            }
        }

        if (standPrefab == null) return;

        // Instanciar el stand como hijo de la imagen trackeada
        GameObject stand = Instantiate(standPrefab, trackedImage.transform);
        stand.SetActive(false);

        // Obtener componentes
        VideoPlayer videoPlayer = stand.GetComponentInChildren<VideoPlayer>();
        AudioSource audioSource = stand.GetComponentInChildren<AudioSource>();

        // Configurar para loop
        if (videoPlayer != null)
        {
            // IMPORTANTE: Configurar el VideoPlayer para renderizar en una textura
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.targetMaterialProperty = "_MainTex"; // O la propiedad que uses
            videoPlayer.Prepare();
        }

        if (audioSource != null)
        {
            audioSource.loop = true;
        }

        // Guardar referencias
        instantiatedStands[imageName] = stand;
        videoPlayers[imageName] = videoPlayer;
        audioSources[imageName] = audioSource;
        standReadyStates[imageName] = false;

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += (source) =>
            {
                standReadyStates[imageName] = true;
                if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                {
                    ActivateStand(imageName);
                }
            };
        }
        else
        {
            standReadyStates[imageName] = true;
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                ActivateStand(imageName);
            }
        }
    }

    private void UpdateStandState(ARTrackedImage trackedImage, string imageName)
    {
        if (!instantiatedStands.ContainsKey(imageName)) return;

        // La posición y rotación ahora se manejan automáticamente por ser hijo del trackedImage
        // Solo necesitamos activar/desactivar según el estado de tracking

        if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
        {
            if (standReadyStates[imageName])
            {
                ActivateStand(imageName);
            }
        }
        else
        {
            DeactivateStand(imageName);
        }
    }

    private void ActivateStand(string imageName)
    {
        if (!instantiatedStands.ContainsKey(imageName)) return;

        GameObject stand = instantiatedStands[imageName];
        stand.SetActive(true);

        if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null)
        {
            if (!vp.isPlaying)
            {
                vp.Play();
            }
        }

        if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null)
        {
            if (!asrc.isPlaying)
            {
                asrc.Play();
            }
        }
    }

    private void DeactivateStand(string imageName)
    {
        if (!instantiatedStands.ContainsKey(imageName)) return;

        GameObject stand = instantiatedStands[imageName];
        stand.SetActive(false);

        if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null)
        {
            if (vp.isPlaying)
            {
                vp.Pause();
            }
        }

        if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null)
        {
            if (asrc.isPlaying)
            {
                asrc.Pause();
            }
        }
    }
}