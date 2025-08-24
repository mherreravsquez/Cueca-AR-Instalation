using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARContentScaler : MonoBehaviour
{
    [Header("Opciones de Escala")]
    public float scaleFactor = 1.0f;
    public bool maintainAspectRatio = true;
    
    private ARTrackedImage parentTrackedImage;
    
    void Start()
    {
        parentTrackedImage = GetComponentInParent<ARTrackedImage>();
        
        if (parentTrackedImage != null)
        {
            AdjustScaleBasedOnImageSize();
        }
    }
    
    void Update()
    {
        // Actualizar escala si el tamaño de la imagen cambia
        if (parentTrackedImage != null && parentTrackedImage.size.magnitude > 0)
        {
            AdjustScaleBasedOnImageSize();
        }
    }
    
    private void AdjustScaleBasedOnImageSize()
    {
        Vector2 imageSize = parentTrackedImage.size;
        
        if (imageSize.magnitude > 0)
        {
            // Calcular escala basada en el tamaño real de la imagen
            float targetScale = Mathf.Max(imageSize.x, imageSize.y) * scaleFactor;
            
            if (maintainAspectRatio)
            {
                transform.localScale = new Vector3(targetScale, targetScale, targetScale);
            }
            else
            {
                transform.localScale = new Vector3(
                    imageSize.x * scaleFactor,
                    imageSize.y * scaleFactor,
                    Mathf.Max(imageSize.x, imageSize.y) * scaleFactor
                );
            }
        }
    }
    
    // Método para ajustar escala manualmente si es necesario
    public void SetScaleFactor(float newScaleFactor)
    {
        scaleFactor = newScaleFactor;
        AdjustScaleBasedOnImageSize();
    }
}
