using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WordWobble : MonoBehaviour {
    private TMP_Text textMesh;
    private Mesh mesh;
    [SerializeField] private Vector3[] vertices;
    [SerializeField] private List<int> wordIndexes;
    [SerializeField] private List<int> wordLengths;
    [SerializeField] public Gradient rainbow;

    private void Start() {
        textMesh = GetComponent<TMP_Text>();
        wordIndexes = new List<int> { 0 };
        wordLengths = new List<int>();
        string s = textMesh.text;
        for (int index = s.IndexOf(' '); index > -1; index = s.IndexOf(' ', index + 1)) {
            wordLengths.Add(index - wordIndexes[wordIndexes.Count - 1]);
            wordIndexes.Add(index + 1);
        }
        wordLengths.Add(s.Length - wordIndexes[wordIndexes.Count - 1]);
    }

    private void Update() {
        textMesh.ForceMeshUpdate();
        mesh = textMesh.mesh;
        vertices = mesh.vertices;
        Color[] colors = mesh.colors;
        for (int w = 0; w < wordIndexes.Count; w++) {
            int wordIndex = wordIndexes[w];
            Vector3 offset = Wobble(Time.time + w);
            for (int i = 0; i < wordLengths[w]; i++) {
                TMP_CharacterInfo c = textMesh.textInfo.characterInfo[wordIndex + i];
                int index = c.vertexIndex;
                colors[index] = rainbow.Evaluate(Mathf.Repeat(Time.time + vertices[index].x * 0.001f, 1f));
                colors[index + 1] = rainbow.Evaluate(Mathf.Repeat(Time.time + vertices[index + 1].x * 0.001f, 1f));
                colors[index + 2] = rainbow.Evaluate(Mathf.Repeat(Time.time + vertices[index + 2].x * 0.001f, 1f));
                colors[index + 3] = rainbow.Evaluate(Mathf.Repeat(Time.time + vertices[index + 3].x * 0.001f, 1f));
                vertices[index] += offset;
                vertices[index + 1] += offset;
                vertices[index + 2] += offset;
                vertices[index + 3] += offset;
            }
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        textMesh.canvasRenderer.SetMesh(mesh);
    }

    private Vector2 Wobble(float time) {
        return new Vector2(Mathf.Sin(time * 3.3f), Mathf.Cos(time * 2.5f));
    }
}