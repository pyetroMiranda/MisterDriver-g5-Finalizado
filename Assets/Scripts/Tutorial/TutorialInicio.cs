using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TutorialInicio : MonoBehaviour
{
    [SerializeField] private GameObject pnlTutorialInicio;
    [SerializeField] private GameObject imgBalaoInicio;
    [SerializeField] private GameObject imgBalaoFreio;
    [SerializeField] private GameObject imgCarroDestaque;
    [SerializeField] private float intervaloAvisos = 1f;
    [SerializeField] private GameObject imgBalaoEsquerda;
    [SerializeField] private GameObject imgBalaoDireita;
    [SerializeField] private GameObject imgBalaoCones;

    private int etapa;
    private float tempo;

    private void Start()
    {
        if (pnlTutorialInicio == null ||
            imgBalaoInicio == null ||
            imgBalaoFreio == null ||
            imgCarroDestaque == null)
        {
            Debug.LogError("Preencha todos os campos do TutorialInicio.", this);
            enabled = false;
            return;
            if (imgBalaoCones != null)
                imgBalaoCones.SetActive(false);
        }

        etapa = 0;
        pnlTutorialInicio.SetActive(true);
        imgBalaoInicio.SetActive(true);
        imgBalaoFreio.SetActive(false);
        imgCarroDestaque.SetActive(true);
        if (imgBalaoEsquerda != null)
            imgBalaoEsquerda.SetActive(false);
        Time.timeScale = 0f;
        if (imgBalaoDireita != null)
            imgBalaoDireita.SetActive(false);
    }

    private void Update()
    {
        bool cima = false;
        bool baixo = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            cima = Keyboard.current.upArrowKey.wasPressedThisFrame;
            baixo = Keyboard.current.downArrowKey.wasPressedThisFrame;
        }
#else
        cima = Input.GetKeyDown(KeyCode.UpArrow);
        baixo = Input.GetKeyDown(KeyCode.DownArrow);
#endif
        bool esquerda = false;

#if ENABLE_INPUT_SYSTEM
if (Keyboard.current != null)
    esquerda = Keyboard.current.leftArrowKey.wasPressedThisFrame;
#else
        esquerda = Input.GetKeyDown(KeyCode.LeftArrow);
#endif

        if (etapa == 4 && esquerda)
        {
            imgBalaoEsquerda.SetActive(false);
            pnlTutorialInicio.SetActive(false);
            Time.timeScale = 1f;
            etapa = 5;
            return;
        }
        bool direita = false;

#if ENABLE_INPUT_SYSTEM
if (Keyboard.current != null)
    direita = Keyboard.current.rightArrowKey.wasPressedThisFrame;
#else
        direita = Input.GetKeyDown(KeyCode.RightArrow);
#endif

        if (etapa == 6 && direita)
        {
            imgBalaoDireita.SetActive(false);
            pnlTutorialInicio.SetActive(false);
            Time.timeScale = 1f;
            etapa = 7;
            return;
        }
        if (etapa == 8 && baixo)
        {
            imgBalaoCones.SetActive(false);
            pnlTutorialInicio.SetActive(false);
            Time.timeScale = 1f;
            etapa = 9;
            return;
        }
        if (etapa == 0 && cima)
        {
            pnlTutorialInicio.SetActive(false);
            Time.timeScale = 1f;
            tempo = 0f;
            etapa = 1;
        }
        else if (etapa == 1)
        {
            tempo += Time.deltaTime;

            if (tempo >= intervaloAvisos)
            {
                imgBalaoInicio.SetActive(false);
                imgBalaoFreio.SetActive(true);

                // No segundo aviso, mostramos o carro real na pista.
                imgCarroDestaque.SetActive(false);

                pnlTutorialInicio.SetActive(true);
                Time.timeScale = 0f;
                etapa = 2;
            }
        }
        else if (etapa == 2 && baixo)
        {
            pnlTutorialInicio.SetActive(false);
            Time.timeScale = 1f;
            etapa = 3;
        }
    }
    public bool MostrarCurvaEsquerda()
    {
        // Só mostra depois da instrução de freio.
        if (etapa != 3 || imgBalaoEsquerda == null)
            return false;

        imgBalaoInicio.SetActive(false);
        imgBalaoFreio.SetActive(false);
        imgCarroDestaque.SetActive(false);
        imgBalaoEsquerda.SetActive(true);

        pnlTutorialInicio.SetActive(true);
        Time.timeScale = 0f;
        etapa = 4;

        return true;
    }
    public bool MostrarCurvaDireita()
    {
        if (etapa != 5 || imgBalaoDireita == null)
            return false;

        imgBalaoInicio.SetActive(false);
        imgBalaoFreio.SetActive(false);
        imgBalaoEsquerda.SetActive(false);
        imgCarroDestaque.SetActive(false);
        imgBalaoDireita.SetActive(true);

        pnlTutorialInicio.SetActive(true);
        Time.timeScale = 0f;
        etapa = 6;

        return true;
    }
    public bool MostrarAvisoCones()
    {
        // Só mostra depois do aviso da curva à direita.
        if (etapa != 7 || imgBalaoCones == null)
            return false;

        imgBalaoInicio.SetActive(false);
        imgBalaoFreio.SetActive(false);

        if (imgBalaoEsquerda != null)
            imgBalaoEsquerda.SetActive(false);

        if (imgBalaoDireita != null)
            imgBalaoDireita.SetActive(false);

        imgCarroDestaque.SetActive(false);
        imgBalaoCones.SetActive(true);
        pnlTutorialInicio.SetActive(true);

        Time.timeScale = 0f;
        etapa = 8;

        return true;
    }
}