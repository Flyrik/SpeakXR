using UnityEngine;
using TMPro; // Assure-toi d'importer le bon namespace pour TextMeshPro

public class VirtualKeyboard : MonoBehaviour
{
    public TMP_InputField inputField; // Utilise TMP_InputField pour TextMeshPro

    public void InsertCharacter(string character)
    {
        if (inputField != null)
        {
            inputField.text += character; // Ajoute le caractère à l'InputField TMP
        }
        else
        {
            Debug.LogError("InputField TMP n'est pas assigné.");
        }
    }

    public void Backspace()
    {
        if (inputField != null && inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1); // Supprime le dernier caractère
        }
    }

    public void Clear()
    {
        if (inputField != null)
        {
            inputField.text = ""; // Vide l'InputField TMP
        }
    }
}