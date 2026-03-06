using TMPro;
using UnityEngine;

public class FontWave : MonoBehaviour
{
    public float amplitude = 5f;
    public float frequency = 2f;

    private TMP_Text text;
    private TMP_TextInfo textInfo;
    private Vector3[][] copyOfVertices;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        text.ForceMeshUpdate();
        textInfo = text.textInfo;

        if (copyOfVertices == null || copyOfVertices.Length != textInfo.meshInfo.Length)
        {
            copyOfVertices = new Vector3[textInfo.meshInfo.Length][];

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                copyOfVertices[i] = textInfo.meshInfo[i].vertices.Clone() as Vector3[];
            }
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            Vector3[] sourceVertices = copyOfVertices[materialIndex];
            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

            float offset = Mathf.Sin(Time.time * frequency + i) * amplitude;

            for (int j = 0; j < 4; j++)
            {
                destinationVertices[vertexIndex + j] =
                    sourceVertices[vertexIndex + j] + new Vector3(0, offset, 0);
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}