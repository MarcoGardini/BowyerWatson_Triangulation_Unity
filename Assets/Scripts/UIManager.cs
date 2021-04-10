using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// manager for all UI components
public class UIManager : MonoBehaviour
{
    public GameObject UIGO, HeaderGO;
    public int amount;
    public Color color;
    RenderTriangle renderTriangle;
    TriangulationManager triangulationManager;
    public Dropdown colorTecniqueDD, triangulationAlgDD;
    public Slider amountSL, redSL, greenSL, blueSL, alphaSL, BGredSL, BGgreenSL, BGblueSL;
    public Text amountN, redN, greenN, blueN, alphaN, FPSN, BGredN, BGgreenN, BGblueN;
    public Toggle calculateAlphaT, invertColorsT;

    void Start()
    {
        triangulationManager = FindObjectOfType<TriangulationManager>();
        renderTriangle = FindObjectOfType<RenderTriangle>();
        colorTecniqueDD.onValueChanged.AddListener    (delegate { OnColorTecnique(colorTecniqueDD.value); });
        triangulationAlgDD.onValueChanged.AddListener (delegate { OnTriangulationAlg(triangulationAlgDD.value); });
        amountSL.onValueChanged.AddListener           (delegate { OnChangeAmount(amountSL.value);});
        redSL.onValueChanged.AddListener              (delegate { OnChangeColor(redSL.value, greenSL.value, blueSL.value, alphaSL.value);});
        greenSL.onValueChanged.AddListener            (delegate { OnChangeColor(redSL.value, greenSL.value, blueSL.value, alphaSL.value);});
        blueSL.onValueChanged.AddListener             (delegate { OnChangeColor(redSL.value, greenSL.value, blueSL.value, alphaSL.value);});
        alphaSL.onValueChanged.AddListener            (delegate { OnChangeColor(redSL.value, greenSL.value, blueSL.value, alphaSL.value);});
        BGredSL.onValueChanged.AddListener            (delegate { OnChangeBackgroundColor(BGredSL.value, BGgreenSL.value, BGblueSL.value);});
        BGgreenSL.onValueChanged.AddListener          (delegate { OnChangeBackgroundColor(BGredSL.value, BGgreenSL.value, BGblueSL.value);});
        BGblueSL.onValueChanged.AddListener           (delegate { OnChangeBackgroundColor(BGredSL.value, BGgreenSL.value, BGblueSL.value);});
        calculateAlphaT.onValueChanged.AddListener    (delegate { OnChangeCalculatedAlpha(calculateAlphaT.isOn);});
        invertColorsT.onValueChanged.AddListener      (delegate { OnChangeInvertColors(invertColorsT.isOn);});
    }

    void OnToggleHeader()
    {
        HeaderGO.SetActive(!HeaderGO.activeSelf);
    }

    void OnToggleUI()
    {
        UIGO.SetActive(!UIGO.activeSelf);
    }

    public void OnColorTecnique(int value)
    {
        renderTriangle.colorTechnique = (RenderTriangle.ColorTechnique)value;
    }

    public void OnTriangulationAlg(int value)
    {
        triangulationManager.triangulationAlg = value;
    }

    public void OnChangeAmount(float value)
    {
        triangulationManager.OnChangeAmount((int)value);
        amountN.text = ((int)value).ToString();
    }

    public void OnChangeColor(float red, float green, float blue, float alpha)
    {
        renderTriangle.OnChangeColor(new Color(red, green, blue, alpha));
        redN.text   = ((int)(red   * 255)).ToString();
        greenN.text = ((int)(green * 255)).ToString();
        blueN.text  = ((int)(blue  * 255)).ToString();
        alphaN.text = ((int)(alpha * 255)).ToString();
    }

    public void OnChangeBackgroundColor(float red, float green, float blue)
    {
        triangulationManager.OnChangeBackgroundColor(new Color(red, green, blue));
        BGredN.text = ((int)(red * 255)).ToString();
        BGgreenN.text = ((int)(green * 255)).ToString();
        BGblueN.text = ((int)(blue * 255)).ToString();
    }

    public void OnChangeCalculatedAlpha(bool value)
    {
        renderTriangle.OnChangeCalculatedAlpha(value);
    }

    public void OnChangeInvertColors(bool value)
    {
        renderTriangle.OnChangeInvertColors(value);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))            
            OnToggleHeader();
        if (Input.GetKeyDown(KeyCode.F2))
            OnToggleUI();

        FPSN.text = "FPS: " + (1f / Time.deltaTime).ToString();
    }
}
