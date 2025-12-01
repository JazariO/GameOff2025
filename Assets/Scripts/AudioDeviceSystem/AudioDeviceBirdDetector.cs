using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class AudioDeviceBirdDetector : MonoBehaviour
{
    [SerializeField] List<Transform> birdsInRange = new();
    [SerializeField] CapsuleCollider bird_detection_capsule;

    [SerializeField] LayerMask bird_detection_layer;

    private void Update()
    {
        Vector3 mic_origin = transform.position;
        Vector3 mic_direction_normalized = transform.up;

        for(int bird_index = 0; bird_index < birdsInRange.Count; bird_index++)
        {
            // project bird position onto the microphone's primary axis
            Vector3 bird_position_world = birdsInRange[bird_index].position;
            float bird_dot_mic_dir = Vector3.Dot(bird_position_world - mic_origin, mic_direction_normalized);
            Vector3 projected_point = mic_origin + mic_direction_normalized * bird_dot_mic_dir;
            Vector3 projection_vector = bird_position_world - projected_point;

            // get the inverse distance to the axis using the capsule radius
            float t = Mathf.InverseLerp(bird_detection_capsule.radius, 0, Vector3.Magnitude(projection_vector));

            // TODO(Jazz): use the t value to drive the audio volume of the bird sounds

            // show debug stuff
            {
                Color heatLine = Color.HSVToRGB(t, 1f, 1f);
                Debug.DrawLine(bird_position_world, projected_point, heatLine);
                TextMeshPro bird_text = birdsInRange[bird_index].gameObject.GetComponentInChildren<TextMeshPro>();
                if(bird_text != null)
                {
                    bird_text.text = $"Distance: {t:0.00}";
                    bird_text.color = heatLine;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // check against layermask for bird
        if(((1 << other.gameObject.layer) & bird_detection_layer) != 0)
        {
            Debug.Log("bird entered audiodevice trigger");

            birdsInRange.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // check against layermask for bird
        if(((1 << other.gameObject.layer) & bird_detection_layer) != 0)
        {
            Debug.Log("bird exited audiodevice trigger");

            if(birdsInRange.Contains(other.transform)) 
            { 
                birdsInRange.Remove(other.transform);
                TextMeshPro bird_text = other.gameObject.GetComponentInChildren<TextMeshPro>();
                if(bird_text != null)
                {
                    bird_text.color = Color.white;
                    bird_text.text = "Out of Range";
                }
            }
        }
    }
}
