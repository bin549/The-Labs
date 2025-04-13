using System.Collections;
using UnityEngine;
using Cinemachine;
using TMPro;

[System.Serializable]
class LabStep {
    public string tip;
    public AudioClip AudioClip;
}

[RequireComponent(typeof(AudioSource))]
public class LabDetector : InteractableItem {
    [SerializeField] private GameObject labActiveUI;
    [SerializeField] private bool isActive = false;
    private string title = "斜面上静摩擦和动摩擦";
    private string introduction = "这是一个模拟箱子被绳子拉着沿着水平面移动的过程。学生可以通过模拟来探索静摩擦和动摩擦的影响，以及它们与表面法向力的关系。";
    private string step = "第一步，第二步，第三步";
    private bool isFinish = false;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LabStep[] labSteps;
    [SerializeField] private GameObject tipUI;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject interationUI;
    [SerializeField] private GameObject finishUI;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private bool isFocus = false;
    [SerializeField] private int currentStep = 0;
    [SerializeField] private GameObject[] allLabItems;
    [SerializeField] private GameObject[] initLabItems;

    public bool IsFocus {
        get => isFocus;
        set => isFocus = value;
    }

    public void IncreateTip() {
        if (this.currentStep == this.labSteps.Length - 1) {
            this.FinishLabItem();
        } else {
            this.currentStep++;
        }
    }

    public CinemachineVirtualCamera VirtualCamera {
        get => virtualCamera;
        set => virtualCamera = value;
    }

    public bool IsActive {
        get { return this.isActive; }
        set { isActive = value; }
    }

    protected override void Awake() {
        base.Awake();
        this.gameManager = GameObject.FindObjectOfType<GameManager>();
        this.audioSource = gameObject.GetComponent<AudioSource>();
    }

    private void Start() {
        this.virtualCamera.enabled = false;
    }

    protected override void Update() {
        base.Update();
        if (Input.GetKeyDown(KeyCode.Escape) && !this.isFocus) {
            this.ExitLab(false);
        }
    }

    private void OnHideLabItems() {
        foreach (var labItem in allLabItems) {
            labItem.SetActive(false);
        }
    }

    private void OnInitLabItems() {
        foreach (var labItem in initLabItems) {
            labItem.SetActive(true);
        }
    }

    protected override void InteractAction() {
        PersonController controller = FindObjectOfType<PersonController>();
        this.Focus(controller);
    }

    protected override void DeactiveAction() {
        if (!this.gameManager.IsBusy) {
            return;
        }
        this.ExitLab(false);
    }

    public void Focus(PersonController controller) {
        this.IsActive = true;
        this.gameManager.IsBusy = true;
        this.labActiveUI.SetActive(true);
        this.interationUI.gameObject.SetActive(false);
        this.gameManager.MuteFootsteps(true);
        PersonCameraController personCameraController = GameObject.FindObjectOfType<PersonCameraController>();
        personCameraController.GetPersonController().mPlayerCamera.gameObject.SetActive(false);
        personCameraController.Cursor.SetActive(false);
        this.OnInitLabItems();
        
        this.virtualCamera.enabled = true;
        base.interactableObject.LineRenderer.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitLab(bool isFinish) {
        this.labActiveUI.SetActive(false);
        this.interationUI.gameObject.SetActive(true);
        StartCoroutine(this.DisableBusy());
        this.gameManager.MuteFootsteps(false);
        PersonCameraController personCameraController = GameObject.FindObjectOfType<PersonCameraController>();
        personCameraController.GetPersonController().mPlayerCamera.gameObject.SetActive(true);
        personCameraController.Cursor.SetActive(true);
        this.OnHideLabItems();
        this.virtualCamera.enabled = false;
        this.IsActive = false;
        this.isFinish = isFinish;
        this.currentStep = 0;
        base.interactableObject.LineRenderer.enabled = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private IEnumerator DisableBusy() {
        yield return new WaitForSeconds(.4f);
        this.gameManager.IsBusy = false;
    }

    public void ShowTip() {
        LabStep labStep = this.labSteps[this.currentStep];
        this.tipText.text = labStep.tip;
        this.audioSource.clip = labStep.AudioClip;
        this.audioSource.Play();
        this.EnableTip(true);
        StartCoroutine(DisenableUI(this.audioSource.clip.length));
    }

    private IEnumerator DisenableUI(float seconds) {
        yield return new WaitForSeconds(seconds);
        this.EnableTip(false);
    }

    private void EnableTip(bool isEnable) {
        this.tipUI.SetActive(isEnable);
    }

    public void FinishLabItem() {
        this.finishUI.gameObject.SetActive(true);
    }

    public void OnContinueBtnDown() {
        this.finishUI.gameObject.SetActive(false);
        this.ExitLab(true);
    }
}