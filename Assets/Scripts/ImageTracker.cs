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
        // Manejar imágenes añadidas
        foreach (var trackedImage in eventArgs.added)
        {
            string imageName = trackedImage.referenceImage.name;
            SetupStandForTrackedImage(trackedImage, imageName);
        }

        // Manejar imágenes actualizadas
        foreach (var trackedImage in eventArgs.updated)
        {
            string imageName = trackedImage.referenceImage.name;
            UpdateStandForTrackedImage(trackedImage, imageName);
        }

        // Manejar imágenes removidas
        foreach (var trackedImage in eventArgs.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            RemoveStand(imageName);
        }
    }

    private void SetupStandForTrackedImage(ARTrackedImage trackedImage, string imageName)
    {
        // Buscar el par imagen-stand correspondiente
        foreach (var pair in imageStandPairs)
        {
            if (pair.imageName == imageName)
            {
                // Instanciar el stand si no existe
                if (!instantiatedStands.ContainsKey(imageName))
                {
                    GameObject stand = Instantiate(pair.standPrefab, trackedImage.transform.position, trackedImage.transform.rotation);
                    stand.SetActive(false); // Inicialmente desactivado

                    // Obtener componentes VideoPlayer y AudioSource
                    VideoPlayer videoPlayer = stand.GetComponentInChildren<VideoPlayer>();
                    AudioSource audioSource = stand.GetComponentInChildren<AudioSource>();

                    // Configurar para que se reproduzcan en bucle
                    if (videoPlayer != null)
                    {
                        videoPlayer.isLooping = true;
                    }
                    if (audioSource != null)
                    {
                        audioSource.loop = true;
                    }

                    // Guardar referencias
                    instantiatedStands[imageName] = stand;
                    videoPlayers[imageName] = videoPlayer;
                    audioSources[imageName] = audioSource;
                }

                // Actualizar la posición y activar
                GameObject standInstance = instantiatedStands[imageName];
                standInstance.transform.position = trackedImage.transform.position;
                standInstance.transform.rotation = trackedImage.transform.rotation;
                standInstance.SetActive(true);

                // Reproducir video y audio
                if (videoPlayers[imageName] != null)
                {
                    videoPlayers[imageName].Play();
                }
                if (audioSources[imageName] != null)
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
            // Actualizar posición y rotación
            stand.transform.position = trackedImage.transform.position;
            stand.transform.rotation = trackedImage.transform.rotation;

            // Activar o desactivar según el estado de seguimiento
            bool isTracking = trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking;
            stand.SetActive(isTracking);

            // Controlar reproducción según el estado
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
        // Cuando se remueve la imagen, desactivamos el stand y pausamos los medios
        if (instantiatedStands.TryGetValue(imageName, out GameObject stand))
        {
            stand.SetActive(false);

            if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null && vp.isPlaying)
                vp.Pause();

            if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null && asrc.isPlaying)
                asrc.Pause();
        }
    }

    // Método para limpiar todos los stands (opcional)
    // public void ClearAllStands()
    // {
    //     foreach (var stand in instantiatedStands.Values)
    //     {
    //         Destroy(stand);
    //     }
    //     
    //     instantiatedStands.Clear();
    //     videoPlayers.Clear();
    //     audioSources.Clear()
    //}
    
}