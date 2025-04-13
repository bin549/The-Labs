using UnityEngine;

public class InteractableDetector : MonoBehaviour {
    public float distanceOpen = 2.5f;
    [SerializeField] private InteractableObject interactableObject = null;
    [SerializeField] private BlinkHandler blinkHandler;

    private void Awake() {
        this.blinkHandler = FindObjectOfType<BlinkHandler>(true);
    }

    private void Update() {
        if (this.interactableObject) {
            this.interactableObject.OnHintToggle(false);
        }
    }

    private void LateUpdate() {
        this.DetectObject();
    }
 
    private void DetectObject() {
        if (!this.blinkHandler.IsOpen) {
            return;
        }
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, this.distanceOpen)) {
            if (hit.transform.GetComponent<InteractableObject>()) {
                this.interactableObject = hit.transform.GetComponent<InteractableObject>();
                this.interactableObject.OnHintToggle(true);
                if (Input.GetKeyDown(KeyCode.E)) {
                    this.OnInteract(hit);
                }
            } 
        }
    }

    private void OnInteract(RaycastHit hit) { 
        if (hit.transform.GetComponent<BulletinBoard>()) {
            GameObject.FindObjectOfType<GameManager>().IsBusy = true;
            BulletinBoard bulletinBoard = hit.transform.GetComponent<BulletinBoard>();
            bulletinBoard.DisplayBulletin(bulletinBoard.BulletinPrefab.activeSelf);
        } else if (hit.transform.GetComponent<Door>()) {
            Door door = hit.transform.GetComponent<Door>();
            GameObject.FindObjectOfType<DoorHandler>().ChangeScene(door);
        } else if (hit.transform.GetComponent<LabDetector>()) {
            GameObject.FindObjectOfType<GameManager>().IsBusy = true;
            LabDetector labDetector = hit.transform.GetComponent<LabDetector>();
            labDetector.Focus(GetComponent<PersonController>());
        }
    }
}