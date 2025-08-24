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
            string imageName = trackedImage.referenceImage.name;
            SetupStandForTrackedImage(trackedImage, imageName);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            string imageName = trackedImage.referenceImage.name;
            UpdateStandForTrackedImage(trackedImage, imageName);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            RemoveStand(imageName);
        }
    }

    private void SetupStandForTrackedImage(ARTrackedImage trackedImage, string imageName)
    {
        foreach (var pair in imageStandPairs)
        {
            if (pair.imageName == imageName)
            {
                if (!instantiatedStands.ContainsKey(imageName))
                {
                    // Instanciar el stand como hijo de la imagen trackeada
                    GameObject stand = Instantiate(pair.standPrefab, trackedImage.transform);
                    
                    // Resetear la posición y rotación local para que esté exactamente sobre la imagen
                    stand.transform.localPosition = Vector3.zero;
                    stand.transform.localRotation = Quaternion.identity;
                    
                    // Ajustar la escala si es necesario (dependiendo del tamaño de tu imagen)
                    // stand.transform.localScale = Vector3.one * 0.1f; // Ajusta este valor según necesites

                    // Obtener los componentes VideoPlayer y AudioSource del stand
                    VideoPlayer videoPlayer = stand.GetComponentInChildren<VideoPlayer>();
                    AudioSource audioSource = stand.GetComponentInChildren<AudioSource>();

                    if (videoPlayer != null)
                    {
                        videoPlayer.isLooping = true;
                        // Configurar el video para renderizar en un material en lugar de la pantalla
                        videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
                    }

                    if (audioSource != null)
                    {
                        audioSource.loop = true;
                    }

                    instantiatedStands[imageName] = stand;
                    videoPlayers[imageName] = videoPlayer;
                    audioSources[imageName] = audioSource;
                    
                    // Activar el stand
                    stand.SetActive(true);
                }
                else
                {
                    // Si ya existe, simplemente actualizamos su padre
                    GameObject stand = instantiatedStands[imageName];
                    stand.transform.SetParent(trackedImage.transform);
                    stand.transform.localPosition = Vector3.zero;
                    stand.transform.localRotation = Quaternion.identity;
                    stand.SetActive(true);
                }

                // Reproducir medios
                if (videoPlayers.ContainsKey(imageName) && videoPlayers[imageName] != null)
                {
                    videoPlayers[imageName].Play();
                }

                if (audioSources.ContainsKey(imageName) && audioSources[imageName] != null)
                {
                    audioSources[imageName].Play();
                }

                break;
            }
        }
    }

    private void UpdateStandForTrackedImage(ARTrackedImage trackedImage, string imageName)
    {
        if (instantiatedStands.TryGetValue(imageName, out GameObject stand))
        {
            bool isTracking = trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking;
            
            // Actualizar la posición y rotación aunque esté trackeando o no
            stand.transform.position = trackedImage.transform.position;
            stand.transform.rotation = trackedImage.transform.rotation;
            
            stand.SetActive(isTracking);

            if (isTracking)
            {
                if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null && !vp.isPlaying)
                    vp.Play();
                if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null && !asrc.isPlaying)
                    asrc.Play();
            }
            else
            {
                if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null && vp.isPlaying)
                    vp.Pause();
                if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null && asrc.isPlaying)
                    asrc.Pause();
            }
        }
    }

    private void RemoveStand(string imageName)
    {
        if (instantiatedStands.TryGetValue(imageName, out GameObject stand))
        {
            // En lugar de destruir, simplemente desactivamos y separamos del padre
            stand.SetActive(false);
            stand.transform.SetParent(null); // Importante: evitar que se destruya con la imagen

            if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null && vp.isPlaying)
                vp.Pause();

            if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null && asrc.isPlaying)
                asrc.Pause();
        }
    }
}