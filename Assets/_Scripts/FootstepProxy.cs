using UnityEngine;

public class FootstepProxy : MonoBehaviour
{
    [SerializeField] private SoundData soundData;
    [SerializeField] private Transform feetPosition;
   
    public void AE_Footstep()
    {
        AudioManager.I.Play(soundData,feetPosition.transform.position);
    }
}
