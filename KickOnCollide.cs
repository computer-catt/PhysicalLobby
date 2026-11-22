using UnityEngine;

namespace PhysicalLobby;

[RequireComponent(typeof(HurtCollider))]
public class KickOnCollide : MonoBehaviour {
    private HurtCollider? _hurtCollider;
    private void Awake() => (_hurtCollider = GetComponent<HurtCollider>()).onImpactPlayer.AddListener(KickSelf);

    public void KickSelf() { // taken straight from menupageesc.cs
        GameDirector.instance.OutroStart();
        ResultScreenUI.instance.Disable();
        NetworkManager.instance.leavePhotonRoom = true;
    }
}