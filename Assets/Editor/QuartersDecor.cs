using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StarshipCabin.EditorTools
{
    /// <summary>
    /// Milestone 6: personal decor.
    ///
    /// Chess set on the low table showing the Morphy "Opera Game" (Morphy vs
    /// Duke Karl of Brunswick and Count Isouard, Paris 1858), position after
    /// 15...Nxd7 — one move before the queen sacrifice 16.Qb8+!! Nxb8 17.Rd8#.
    /// White faces the couch, so the mate-in-two sits in view of the stars.
    ///
    /// Library on the desk: a flat stack (Bible, The Divine Comedy, Epitome
    /// Historiae Sacrae on top with its cover label facing up) plus Fischer's
    /// "My 60 Memorable Games" and Illich's "Tools for Conviviality" standing
    /// with spine labels, next to the lamp. Titles use legacy TextMesh with
    /// the built-in runtime font — legible up close at the desk anchor.
    ///
    /// Geometry economy: all 21 pieces of one colour share a single mesh via
    /// MeshDraft + AppendChamferedBox; the board squares are two meshes
    /// (light/dark); the whole vignette adds a handful of draw calls.
    /// </summary>
    internal static class QuartersDecor
    {
        // Board geometry.
        private const float Square = 0.04f;
        private static readonly Vector3 BoardCenter = new(-1.70f, 0.401f, 0.02f); // on the low table (top 0.39)
        private const float SquareY = 0.4115f;
        private const float PieceBaseY = 0.412f;

        public static void Build(Transform parent)
        {
            var ivory = QuartersSceneSetup.CreateMaterial("Chess Ivory", new Color(0.894f, 0.855f, 0.761f));
            var ebony = QuartersSceneSetup.CreateMaterial("Chess Ebony", new Color(0.165f, 0.129f, 0.106f));
            var boardLight = QuartersSceneSetup.CreateMaterial("Chess Board Light", new Color(0.839f, 0.792f, 0.694f));
            var boardDark = QuartersSceneSetup.CreateMaterial("Chess Board Dark", new Color(0.290f, 0.220f, 0.165f));
            var wood = QuartersSceneSetup.CreateMaterial("Cabin Wood", new Color(0.541f, 0.416f, 0.282f));

            BuildChessSet(parent, ivory, ebony, boardLight, boardDark, wood);
            BuildLibrary(parent);
        }

        // ------------------------------------------------------------------
        // Chess: the Opera Game, one move before the queen sac
        // ------------------------------------------------------------------

        private static void BuildChessSet(
            Transform parent, Material ivory, Material ebony, Material boardLight, Material boardDark, Material wood)
        {
            // Board frame.
            var frame = QuartersMeshes.ChamferedBox("Quarters Chess Board Frame", 0.37f, 0.018f, 0.37f, 0.006f);
            QuartersSceneSetup.MeshObject(parent, "Chess Board", frame, wood, BoardCenter, Quaternion.identity);

            // Squares: two drafts, a1 dark at White's near-left (White faces the couch at -Z).
            var light = new MeshDraft();
            var dark = new MeshDraft();

            for (var file = 0; file < 8; file++)
            {
                for (var rank = 0; rank < 8; rank++)
                {
                    var center = SquareCenter(file, rank);
                    const float h = 0.0395f * 0.5f;
                    var p00 = center + new Vector3(-h, 0f, -h);
                    var p10 = center + new Vector3(h, 0f, -h);
                    var p11 = center + new Vector3(h, 0f, h);
                    var p01 = center + new Vector3(-h, 0f, h);

                    var draft = (file + rank) % 2 == 0 ? dark : light;
                    draft.AddQuadOriented(p00, p10, p11, p01, Vector3.up);
                }
            }

            QuartersSceneSetup.MeshObject(parent, "Chess Squares Light", light.ToMesh("Quarters Chess Squares Light"), boardLight);
            QuartersSceneSetup.MeshObject(parent, "Chess Squares Dark", dark.ToMesh("Quarters Chess Squares Dark"), boardDark);

            // Opera Game, Paris 1858 — after 15...Nxd7, before 16.Qb8+!! Nxb8 17.Rd8#.
            var whitePieces = new[] { "K c1", "Q b3", "R d1", "B g5", "P a2", "P b2", "P c2", "P e4", "P f2", "P g2", "P h2" };
            var blackPieces = new[] { "K e8", "Q e6", "R h8", "B f8", "N d7", "P a7", "P e5", "P f7", "P g7", "P h7" };

            var whiteDraft = new MeshDraft();
            var blackDraft = new MeshDraft();

            foreach (var spec in whitePieces)
            {
                AddPiece(whiteDraft, spec, facing: 1f);
            }

            foreach (var spec in blackPieces)
            {
                AddPiece(blackDraft, spec, facing: -1f);
            }

            QuartersSceneSetup.MeshObject(parent, "Chess Pieces White", whiteDraft.ToMesh("Quarters Chess Pieces White"), ivory);
            QuartersSceneSetup.MeshObject(parent, "Chess Pieces Black", blackDraft.ToMesh("Quarters Chess Pieces Black"), ebony);
        }

        private static Vector3 SquareCenter(int file, int rank)
        {
            // Files a..h along +X; ranks 1..8 along +Z (White's back rank
            // nearest the couch). a1 = (0,0) is dark: (0+0) % 2 == 0. ✓
            return new Vector3(
                BoardCenter.x + (file - 3.5f) * Square,
                SquareY,
                BoardCenter.z + (rank - 3.5f) * Square);
        }

        /// <summary>Parses "K c1" style specs and appends a stylized piece.</summary>
        private static void AddPiece(MeshDraft draft, string spec, float facing)
        {
            var type = spec[0];
            var file = spec[2] - 'a';
            var rank = spec[3] - '1';
            var square = SquareCenter(file, rank);
            var basePos = new Vector3(square.x, PieceBaseY, square.z);

            void Part(float y, float sx, float sy, float sz, float ch)
            {
                QuartersMeshes.AppendChamferedBox(draft, basePos + Vector3.up * y, new Vector3(sx, sy, sz), ch);
            }

            switch (type)
            {
                case 'P':
                    Part(0.003f, 0.020f, 0.006f, 0.020f, 0.006f);
                    Part(0.013f, 0.012f, 0.014f, 0.012f, 0.004f);
                    Part(0.025f, 0.014f, 0.010f, 0.014f, 0.006f);
                    break;
                case 'R':
                    Part(0.003f, 0.022f, 0.006f, 0.022f, 0.006f);
                    Part(0.017f, 0.016f, 0.022f, 0.016f, 0.003f);
                    Part(0.032f, 0.020f, 0.008f, 0.020f, 0.002f);
                    break;
                case 'N':
                    Part(0.003f, 0.022f, 0.006f, 0.022f, 0.006f);
                    Part(0.014f, 0.014f, 0.016f, 0.014f, 0.004f);
                    // Slanted head, leaning toward the opponent.
                    QuartersMeshes.AppendChamferedBox(
                        draft,
                        basePos + new Vector3(0f, 0.030f, facing * 0.004f),
                        new Vector3(0.012f, 0.020f, 0.020f),
                        0.004f,
                        Quaternion.Euler(facing * 22f, 0f, 0f)); // positive X-rotation leans the head toward +Z (the opponent, for White)
                    break;
                case 'B':
                    Part(0.003f, 0.022f, 0.006f, 0.022f, 0.006f);
                    Part(0.016f, 0.014f, 0.020f, 0.014f, 0.004f);
                    Part(0.032f, 0.010f, 0.012f, 0.010f, 0.005f);
                    Part(0.042f, 0.008f, 0.008f, 0.008f, 0.004f);
                    break;
                case 'Q':
                    Part(0.003f, 0.024f, 0.006f, 0.024f, 0.006f);
                    Part(0.019f, 0.016f, 0.026f, 0.016f, 0.005f);
                    Part(0.036f, 0.018f, 0.008f, 0.018f, 0.007f);
                    Part(0.044f, 0.008f, 0.008f, 0.008f, 0.004f);
                    break;
                case 'K':
                    Part(0.003f, 0.024f, 0.006f, 0.024f, 0.006f);
                    Part(0.021f, 0.016f, 0.030f, 0.016f, 0.005f);
                    Part(0.040f, 0.018f, 0.008f, 0.018f, 0.004f);
                    Part(0.050f, 0.004f, 0.012f, 0.004f, 0.001f); // cross vertical
                    Part(0.050f, 0.010f, 0.004f, 0.004f, 0.001f); // cross horizontal
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Library on the desk
        // ------------------------------------------------------------------

        private static void BuildLibrary(Transform parent)
        {
            var leatherRed = QuartersSceneSetup.CreateMaterial("Book Leather Red", new Color(0.353f, 0.180f, 0.165f));
            var teal = QuartersSceneSetup.CreateMaterial("Book Deep Teal", new Color(0.137f, 0.259f, 0.306f));
            var tan = QuartersSceneSetup.CreateMaterial("Book Aged Tan", new Color(0.706f, 0.608f, 0.424f));
            var green = QuartersSceneSetup.CreateMaterial("Book Dark Green", new Color(0.133f, 0.224f, 0.173f));
            var orange = QuartersSceneSetup.CreateMaterial("Book Warm Orange", new Color(0.690f, 0.286f, 0.184f));

            const float deskTop = 0.745f;
            var gold = new Color(0.85f, 0.70f, 0.28f);
            var cream = new Color(0.93f, 0.90f, 0.82f);
            var darkInk = new Color(0.22f, 0.17f, 0.10f);

            // ---- Flat stack: Bible (bottom), The Divine Comedy, Epitome (top).
            // Spines face +X (into the room / toward the desk stool).
            var stackX = -2.70f;
            var stackZ = 2.25f;

            Book(parent, leatherRed, "Book: Holy Bible",
                new Vector3(0.185f, 0.055f, 0.25f), new Vector3(stackX, deskTop + 0.0275f, stackZ), yaw: 4f);
            Label(parent, "HOLY  BIBLE", gold, 0.0042f,
                new Vector3(stackX + 0.094f, deskTop + 0.0275f, stackZ),
                Quaternion.Euler(0f, 4f, 0f) * Quaternion.LookRotation(Vector3.left, Vector3.up));

            var comedyY = deskTop + 0.055f + 0.0225f;
            Book(parent, teal, "Book: The Divine Comedy",
                new Vector3(0.165f, 0.045f, 0.22f), new Vector3(stackX - 0.005f, comedyY, stackZ + 0.01f), yaw: -6f);
            Label(parent, "LA DIVINA COMMEDIA · DANTE", cream, 0.0032f,
                new Vector3(stackX - 0.005f + 0.084f, comedyY, stackZ + 0.01f),
                Quaternion.Euler(0f, -6f, 0f) * Quaternion.LookRotation(Vector3.left, Vector3.up));

            var epitomeY = deskTop + 0.055f + 0.045f + 0.014f;
            Book(parent, tan, "Book: Epitome Historiae Sacrae",
                new Vector3(0.13f, 0.028f, 0.18f), new Vector3(stackX + 0.008f, epitomeY, stackZ - 0.008f), yaw: 9f);
            // Cover label faces up, top of the text toward the wall.
            Label(parent, "EPITOME\nHISTORIAE  SACRAE", darkInk, 0.0036f,
                new Vector3(stackX + 0.008f, epitomeY + 0.0155f, stackZ - 0.008f),
                Quaternion.Euler(0f, 9f, 0f) * Quaternion.LookRotation(Vector3.down, Vector3.left));

            // ---- Standing pair near the lamp: Fischer upright, Illich leaning on it.
            Book(parent, green, "Book: My 60 Memorable Games",
                new Vector3(0.16f, 0.235f, 0.045f), new Vector3(-2.62f, deskTop + 0.1175f, 2.02f), yaw: 0f);
            Label(parent, "MY 60 MEMORABLE GAMES", cream, 0.0028f,
                new Vector3(-2.62f + 0.081f, deskTop + 0.1175f, 2.02f),
                Quaternion.LookRotation(Vector3.left, Vector3.up) * Quaternion.Euler(0f, 0f, -90f));

            var illichTilt = Quaternion.Euler(8f, 0f, 0f); // leans toward Fischer
            Book(parent, orange, "Book: Tools for Conviviality",
                new Vector3(0.14f, 0.21f, 0.04f), new Vector3(-2.63f, deskTop + 0.104f, 1.955f), illichTilt);
            Label(parent, "TOOLS FOR CONVIVIALITY", cream, 0.0026f,
                new Vector3(-2.63f + 0.071f, deskTop + 0.104f, 1.958f),
                illichTilt * Quaternion.LookRotation(Vector3.left, Vector3.up) * Quaternion.Euler(0f, 0f, -90f));
        }

        private static void Book(Transform parent, Material material, string name, Vector3 size, Vector3 position, float yaw)
        {
            Book(parent, material, name, size, position, Quaternion.Euler(0f, yaw, 0f));
        }

        private static void Book(Transform parent, Material material, string name, Vector3 size, Vector3 position, Quaternion rotation)
        {
            var mesh = QuartersMeshes.ChamferedBox($"Quarters {AssetSafeName(name)}", size.x, size.y, size.z, 0.004f);
            QuartersSceneSetup.MeshObject(parent, name, mesh, material, position, rotation);
        }

        private static string AssetSafeName(string value)
        {
            return value.Replace(':', '-').Replace('/', '-').Replace('\\', '-');
        }

        /// <summary>
        /// Legacy TextMesh label with the built-in runtime font. Convention:
        /// the rotation's forward axis points AWAY from the reader.
        /// </summary>
        private static void Label(Transform parent, string text, Color color, float characterSize, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject($"Label: {text.Replace("\n", " ")}");
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.rotation = rotation;

            var textMesh = go.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = textMesh.font.material;

            // Dynamic font atlas + transparent shader: exclude from static
            // batching and GI.
            GameObjectUtility.SetStaticEditorFlags(go, 0);
        }
    }
}
