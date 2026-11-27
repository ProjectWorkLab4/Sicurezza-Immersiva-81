using UnityEngine;

/// <summary>
/// Represents a clickable square that responds to poke interaction.
/// When poked, it changes color to green.
/// Each square can only be clicked once.
/// </summary>
public class ClickableSquare : MonoBehaviour
{
    /// <summary>The color to display when the square has been clicked (green by default).</summary>
    [SerializeField] Color greenColor = new Color(0.2f, 0.8f, 0.2f);
 
    /// <summary>The renderer component for changing the material color.</summary>
    Renderer objectRenderer;
    
    /// <summary>Stores the original color of the object before it was clicked.</summary>
    Color originalColor;
    
    /// <summary>Tracks whether this square has already been clicked.</summary>
    bool used;
 
    void OnEnable()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
            originalColor = objectRenderer.material.color;
        used = false;
    }
 
    void OnDisable()
    {
        if (objectRenderer != null)
            objectRenderer.material.color = originalColor;
    }

    /// <summary>Called when a collider enters the trigger (poke starts).</summary>
    void OnTriggerEnter(Collider other)
    {
        if (used) return;
        
        // Check if the collider is from a hand/poke interactor
        if (other.CompareTag("hand") || other.name.Contains("Poke") || other.name.Contains("Hand"))
        {
            OnPokeEnter();
        }
    }

    /// <summary>Called when a poke interaction begins.</summary>
    void OnPokeEnter()
    {
        used = true;
        if (objectRenderer != null)
            objectRenderer.material.color = greenColor;
    }

    /// <summary>Called when a poke interaction ends.</summary>
    public void OnPokeExit()
    {
    }
}