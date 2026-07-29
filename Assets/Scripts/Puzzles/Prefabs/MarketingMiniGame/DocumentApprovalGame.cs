using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class DocumentApprovalGame : MonoBehaviour, IPuzzle
{
    [Header("UI References")]
    [SerializeField] private TMP_Text companyTitle;
    [SerializeField] private TMP_Text requestTitle;
    [SerializeField] private TMP_Text requestNumber;
    [SerializeField] private TMP_Text requestBody;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text reasonText;

    [SerializeField] private Image signatureImage;
    [SerializeField] private Image stampImage;
    [SerializeField] private Image photoImage;

    [SerializeField] private Button approveButton;
    [SerializeField] private Button rejectButton;

    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Sprites")]
    [SerializeField] private Sprite logoSprite;
    [SerializeField] private Sprite stampApproveSprite;
    [SerializeField] private Sprite stampRejectSprite;
    [SerializeField] private Sprite photoSprite;
    [SerializeField] private Sprite signatureSprite;

    [Header("Timing")]
    [SerializeField] private float signatureAppearDelay = 0.6f;
    [SerializeField] private float stampAnimationDelay = 0.8f;
    [SerializeField] private float feedbackDisplayTime = 1.2f;

    

    private (string request, bool shouldApprove)[] budgetRequests;
    private bool shouldApproveCurrent;
    private bool inputLocked;

    private int correctCount = 0;
    private int totalCount = 0;
    private const int TotalDocsPerRound = 5;
    public bool IsCompleted { get; private set; }

    void Awake()
    {
        InitRequests();
        approveButton.onClick.AddListener(() => OnDecision(true));
        rejectButton.onClick.AddListener(() => OnDecision(false));
        feedbackPanel.SetActive(false);
        stampImage.gameObject.SetActive(false);
        signatureImage.gameObject.SetActive(false);

        
    }

    void Start()
    {
       
    }

    public void Begin()
    {
        IsCompleted = false;
        correctCount = 0;
        totalCount = 0;
        SpawnDocument();
    }

    public void ForceEnd()
    {
        IsCompleted = true;
    }

    public float GetLocalScore() => totalCount == 0 ? 0f : (float)correctCount / TotalDocsPerRound;

    void InitRequests()
    {
        budgetRequests = new (string, bool)[]
        {
            ("Закупка 500 золотых ручек для отдела кадров", false),
            ("Покраска офиса в цвет настроения CEO", false),
            ("Покупка 3D-принтера для печати печенья", false),
            ("Аренда вертолета для доставки кофе", false),
            ("Закупка 200 надувных единорогов для коридора", false),
            ("Покупка личного массажиста для степлера", false),
            ("Бюджет на ежемесячный корпоратив в Дубае", false),
            ("Закупка 1000 пакетиков чая 'Для бедных'", true),
            ("Покупка нового принтера (старый горит)", true),
            ("Ремонт кофемашины (все умирают без кофе)", true),
            ("Закупка бумаги A4 для печати документов", true),
            ("Покупка стульев (сотрудники сидят на коробках)", true),
            ("Замена лампочек (работаем в темноте 3 недели)", true),
            ("Закупка туалетной бумаги (критически важно)", true),
            ("Покупка Wi-Fi роутера (интернет через голубей)", true),
            ("Бюджет на обучение 'Как не гореть на работе'", false),
            ("Закупка личного бариста для каждого сотрудника", false),
            ("Покупка золотого унитаза для VIP-туалета", false),
            ("Аренда яхты для 'командообразования'", false),
            ("Закупка 50 гамаков для open-space", false),
        };
    }

    void SpawnDocument()
    {
        inputLocked = false;
        stampImage.gameObject.SetActive(false);
        signatureImage.gameObject.SetActive(false);
        feedbackPanel.SetActive(false);

        var request = budgetRequests[Random.Range(0, budgetRequests.Length)];
        shouldApproveCurrent = request.shouldApprove;

        companyTitle.text = "ООО \"БЮРОКРАТИЯ\"";
        requestTitle.text = "ЗАЯВКА НА БЮДЖЕТ";
        requestNumber.text = "№" + Random.Range(10000, 99999) + " от " + System.DateTime.Now.ToString("dd.MM.yyyy");
        requestBody.text = request.request;

        int amount = shouldApproveCurrent ? Random.Range(500, 5000) : Random.Range(50000, 500000);
        amountText.text = "СУММА: " + amount.ToString("N0") + " Р";

        string[] goodReasons = {
            "Обоснование: Критически необходимо для работы.",
            "Обоснование: Без этого отдел остановится.",
            "Обоснование: Закон требует (probably).",
            "Обоснование: Уже 3 месяца терпим."
        };
        string[] badReasons = {
            "Обоснование: Потому что я CEO.",
            "Обоснование: Для повышения морального духа.",
            "Обоснование: Это инвестиция в будущее (maybe).",
            "Обоснование: Видел у конкурентов — завидую."
        };
        reasonText.text = shouldApproveCurrent
            ? goodReasons[Random.Range(0, goodReasons.Length)]
            : badReasons[Random.Range(0, badReasons.Length)];

        photoImage.gameObject.SetActive(Random.value > 0.5f);
        if (photoImage.gameObject.activeSelf) photoImage.sprite = photoSprite;
    }

    void OnDecision(bool approved)
    {
        if (inputLocked) return;
        inputLocked = true;

        StartCoroutine(DecisionSequence(approved));
    }

    IEnumerator DecisionSequence(bool approved)
    {
        stampImage.sprite = approved ? stampApproveSprite : stampRejectSprite;
        stampImage.rectTransform.rotation = (Quaternion.Euler(0f,0f, (Random.value - 0.5f) * 15f));
        stampImage.gameObject.SetActive(true);
        signatureImage.sprite = signatureSprite;
        signatureImage.gameObject.SetActive(true);

        yield return new WaitForSeconds(stampAnimationDelay);

        bool wasCorrect = (approved == shouldApproveCurrent);
        Evaluate(wasCorrect);

        yield return new WaitForSeconds(feedbackDisplayTime);

        if (totalCount >= TotalDocsPerRound)
        {
            IsCompleted = true;
        }
        else
        {
            SpawnDocument();
        }
    }

    void Evaluate(bool wasCorrect)
    {
        totalCount++;
        if (wasCorrect)
        {
            correctCount++;
            MiniGameEventSystem.Instance.ReportCorrectAction(NetworkManager.Singleton.LocalClientId);
        }

        feedbackText.text = wasCorrect ? "Верно!" : "Ошибка!";
        feedbackText.color = wasCorrect ? Color.green : Color.red;
        feedbackPanel.SetActive(true);
    }
}