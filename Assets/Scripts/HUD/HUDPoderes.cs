using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de poderes no HUD: um "slot" para cada poder comprado, com a tecla,
// o nome e uma barrinha de recarga. Brilha em ciano enquanto o poder está ativo.
public class HUDPoderes : MonoBehaviour
{
    [SerializeField] private Transform lista;
    [SerializeField] private GameObject modeloSlot;

    [Header("Cores")]
    [SerializeField] private Color corPronto = new Color(1f, 0.12f, 0.8f);       // magenta
    [SerializeField] private Color corAtivo = new Color(0.18f, 0.89f, 0.9f);     // ciano
    [SerializeField] private Color corRecarga = new Color(0.45f, 0.4f, 0.55f);   // cinza

    private class Slot
    {
        public IPoder poder;
        public Image moldura;
        public Image barra;
        public TMP_Text nome;
        public TMP_Text tecla;
        public string ultimaInfo;
        public bool estavaPronto;
        public RectTransform rt;
    }

    private readonly List<Slot> slots = new List<Slot>();

    private void Start()
    {
        CarroController carro = FindFirstObjectByType<CarroController>();
        if (carro == null || lista == null || modeloSlot == null)
        {
            gameObject.SetActive(false);
            return;
        }

        modeloSlot.SetActive(false);

        foreach (IPoder poder in carro.GetComponentsInChildren<IPoder>())
        {
            if (!poder.Comprado) continue;

            GameObject obj = Instantiate(modeloSlot, lista);
            obj.SetActive(true);
            obj.name = "Slot " + poder.Nome;

            Slot slot = new Slot
            {
                poder = poder,
                rt = obj.GetComponent<RectTransform>(),
                moldura = obj.GetComponent<Image>(),
                barra = obj.transform.Find("Barra/Preenchimento")?.GetComponent<Image>(),
                nome = obj.transform.Find("TxtNome")?.GetComponent<TMP_Text>(),
                tecla = obj.transform.Find("TxtTecla")?.GetComponent<TMP_Text>(),
            };

            slot.ultimaInfo = null;
            slot.estavaPronto = poder.Pronto;
            slots.Add(slot);
        }

        if (slots.Count == 0) gameObject.SetActive(false);
    }

    private void Update()
    {
        foreach (Slot slot in slots)
        {
            IPoder p = slot.poder;
            Color cor = p.Ativo ? corAtivo : (p.Pronto ? corPronto : corRecarga);

            if (slot.barra != null)
            {
                slot.barra.fillAmount = Mathf.Clamp01(p.Progresso);
                slot.barra.color = cor;
            }
            if (slot.tecla != null) slot.tecla.color = p.Pronto || p.Ativo ? Color.white : corRecarga;

            // Textos só mudam quando a informação muda.
            // Passivo (sem tecla): a info vai no lugar da tecla (ex.: "x3"); senão, ao lado do nome.
            if (slot.ultimaInfo != p.Info)
            {
                slot.ultimaInfo = p.Info;
                bool passivo = string.IsNullOrEmpty(p.Tecla);
                if (slot.tecla != null) slot.tecla.text = passivo ? p.Info : p.Tecla;
                if (slot.nome != null)
                    slot.nome.text = passivo || string.IsNullOrEmpty(p.Info)
                        ? p.Nome
                        : p.Nome + " <color=#2DE2E6>" + p.Info + "</color>";
            }

            // Ativo: moldura pulsa em ciano
            if (slot.moldura != null)
                slot.moldura.color = p.Ativo
                    ? Color.Lerp(Color.white, corAtivo, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f))
                    : Color.white;

            // Acabou de ficar pronto: o slot dá um "pulo"
            if (p.Pronto && !slot.estavaPronto && slot.rt != null) slot.rt.localScale = Vector3.one * 1.15f;
            if (slot.rt != null) slot.rt.localScale = Vector3.Lerp(slot.rt.localScale, Vector3.one, 10f * Time.unscaledDeltaTime);
            slot.estavaPronto = p.Pronto;
        }
    }
}
