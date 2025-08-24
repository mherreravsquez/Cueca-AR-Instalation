using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Video;

public class ImageTracker : MonoBehaviour
{
    // Estructura que define la relación entre una imagen y su stand correspondiente
    [System.Serializable]
    public struct ImageStandPair
    {
        public string imageName;      // Nombre de la imagen en la Reference Image Library
        public GameObject standPrefab; // Prefab del stand que se instanciará para esta imagen
    }

    // Referencia al ARTrackedImageManager que gestiona el tracking de imágenes
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    
    // Lista de pares que asocian imágenes con sus stands correspondientes
    [SerializeField] private List<ImageStandPair> imageStandPairs;

    // Diccionarios para mantener referencias a los objetos instanciados y sus componentes
    private Dictionary<string, GameObject> instantiatedStands = new Dictionary<string, GameObject>();
    private Dictionary<string, VideoPlayer> videoPlayers = new Dictionary<string, VideoPlayer>();
    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    // Suscribirse a los eventos cuando el objeto se habilita
    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    // Desuscribirse de los eventos cuando el objeto se deshabilita
    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    // Método que se ejecuta cuando cambia el estado de las imágenes trackeadas
    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Procesar las imágenes recién detectadas
        foreach (var trackedImage in eventArgs.added)
        {
            string imageName = trackedImage.referenceImage.name;
            SetupStandForTrackedImage(trackedImage, imageName);
        }

        // Procesar las imágenes actualizadas (posición o estado de tracking)
        foreach (var trackedImage in eventArgs.updated)
        {
            string imageName = trackedImage.referenceImage.name;
            UpdateStandForTrackedImage(trackedImage, imageName);
        }

        // Procesar las imágenes que dejaron de ser detectadas
        foreach (var trackedImage in eventArgs.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            RemoveStand(imageName);
        }
    }

    // Configurar un stand para una imagen recién detectada
    private void SetupStandForTrackedImage(ARTrackedImage trackedImage, string imageName)
    {
        // Buscar en la lista de pares la imagen actual
        foreach (var pair in imageStandPairs)
        {
            if (pair.imageName == imageName)
            {
                // Si no hemos creado un stand para esta imagen, lo instanciamos
                if (!instantiatedStands.ContainsKey(imageName))
                {
                    // Instanciar el stand en la posición y rotación de la imagen detectada
                    GameObject stand = Instantiate(pair.standPrefab, trackedImage.transform.position, trackedImage.transform.rotation);
                    stand.SetActive(false); // Inicialmente desactivado

                    // Obtener los componentes VideoPlayer y AudioSource del stand
                    VideoPlayer videoPlayer = stand.GetComponentInChildren<VideoPlayer>();
                    AudioSource audioSource = stand.GetComponentInChildren<AudioSource>();

                    // Configurar el VideoPlayer para que se reproduzca en bucle
                    if (videoPlayer != null)
                    {
                        videoPlayer.isLooping = true;
                    }

                    // Configurar el AudioSource para que se reproduzca en bucle
                    if (audioSource != null)
                    {
                        audioSource.loop = true;
                    }

                    // Guardar las referencias en los diccionarios
                    instantiatedStands[imageName] = stand;
                    videoPlayers[imageName] = videoPlayer;
                    audioSources[imageName] = audioSource;
                }

                // Obtener la instancia del stand para esta imagen
                GameObject standInstance = instantiatedStands[imageName];
                // Actualizar la posición y rotación del stand
                standInstance.transform.position = trackedImage.transform.position;
                standInstance.transform.rotation = trackedImage.transform.rotation;
                // Activar el stand
                standInstance.SetActive(true);

                // Reproducir el video si existe
                if (videoPlayers[imageName] != null)
                {
                    videoPlayers[imageName].Play();
                }

                // Reproducir el audio si existe
                if (audioSources[imageName] != null)
                {
                    audioSources[imageName].Play();
                }

                break; // Salir del bucle una vez encontrado el par
            }
        }
    }

    // Actualizar el stand para una imagen cuyo estado ha cambiado
    private void UpdateStandForTrackedImage(ARTrackedImage trackedImage, string imageName)
    {
        // Verificar si existe un stand para esta imagen
        if (instantiatedStands.TryGetValue(imageName, out GameObject stand))
        {
            // Actualizar la posición y rotación del stand
            stand.transform.position = trackedImage.transform.position;
            stand.transform.rotation = trackedImage.transform.rotation;

            // Determinar si la imagen está siendo trackeada correctamente
            bool isTracking = trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking;
            
            // Activar o desactivar el stand según el estado de tracking
            stand.SetActive(isTracking);

            // Controlar la reproducción de video y audio según el estado
            if (isTracking)
            {
                // Si está trackeando, reproducir si no lo está haciendo
                if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null && !vp.isPlaying)
                    vp.Play();
                if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null && !asrc.isPlaying)
                    asrc.Play();
            }
            else
            {
                // Si no está trackeando, pausar la reproducción
                if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null && vp.isPlaying)
                    vp.Pause();
                if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null && asrc.isPlaying)
                    asrc.Pause();
            }
        }
    }

    // Remover/Desactivar el stand cuando la imagen deja de ser trackeada
    private void RemoveStand(string imageName)
    {
        // Verificar si existe un stand para esta imagen
        if (instantiatedStands.TryGetValue(imageName, out GameObject stand))
        {
            // Desactivar el stand
            stand.SetActive(false);

            // Pausar el video si está reproduciendo
            if (videoPlayers.TryGetValue(imageName, out VideoPlayer vp) && vp != null && vp.isPlaying)
                vp.Pause();

            // Pausar el audio si está reproduciendo
            if (audioSources.TryGetValue(imageName, out AudioSource asrc) && asrc != null && asrc.isPlaying)
                asrc.Pause();
        }
    }

    // Método público para limpiar todos los stands (útil para reinicios o cambios de escena)
    public void ClearAllStands()
    {
        // Destruir todos los stands instanciados
        foreach (var stand in instantiatedStands.Values)
        {
            Destroy(stand);
        }

        // Limpiar los diccionarios
        instantiatedStands.Clear();
        videoPlayers.Clear();
        audioSources.Clear();
    }
}