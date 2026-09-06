using System.Collections.Generic;
using Actors.Player;
using UnityEngine;

namespace Actors.Brains{
    /// <summary>
    /// Handles camera-based screen-to-world ray casting and marquee box intersection
    /// for character selection and selectable object inspection.
    /// </summary>
    public static class SelectionScanner{
        /// <summary>
        /// Raycasts from a screen position to detect a Character on the specified layer.
        /// </summary>
        public static Character RaycastCharacter(Camera cam, Vector2 screenPos, LayerMask layer, float maxDistance = 500f){
            Ray ray = cam.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out RaycastHit hit, maxDistance, layer) ? hit.collider.GetComponentInParent<Character>() : null;
        }

        /// <summary>
        /// Raycasts from a screen position to detect any ISelectable object on the specified layer.
        /// </summary>
        public static ISelectable RaycastSelectable(Camera cam, Vector2 screenPos, LayerMask layer, float maxDistance = 500f){
            Ray ray = cam.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out RaycastHit hit, maxDistance, layer) ? hit.collider.GetComponentInParent<ISelectable>() : null;
        }

        /// <summary>
        /// Gathers all characters from a candidate roster whose screen-projected positions
        /// fall within a 2D screen-space rectangle.
        /// </summary>
        public static List<Character> GetCharactersInScreenRect(Camera cam, Vector2 p1, Vector2 p2, List<Character> candidates){
            Rect            selectionRect = GetScreenRect(p1, p2);
            List<Character> enclosed      = new();

            foreach (Character character in candidates){
                Vector3   screenPoint = cam.WorldToScreenPoint(character.transform.position);

                if (screenPoint.z > 0 && selectionRect.Contains(new Vector2(screenPoint.x, screenPoint.y)))
                    enclosed.Add(character);
            }

            return enclosed;
        }

        public static Rect GetScreenRect(Vector2 p1, Vector2 p2){
            float xMin = Mathf.Min(p1.x, p2.x);
            float xMax = Mathf.Max(p1.x, p2.x);
            float yMin = Mathf.Min(p1.y, p2.y);
            float yMax = Mathf.Max(p1.y, p2.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public static bool IsPointerInsideViewport(Vector2 screenPos) =>
            screenPos.x >= 0 && screenPos.x <= Screen.width && screenPos.y >= 0 && screenPos.y <= Screen.height;

        public static void DrawScreenRect(Rect rect, Color color){
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void DrawScreenRectBorder(Rect rect, float thickness, Color color){
            DrawScreenRect(new Rect(rect.xMin,             rect.yMin,             rect.width, thickness),   color);
            DrawScreenRect(new Rect(rect.xMin,             rect.yMax - thickness, rect.width, thickness),   color);
            DrawScreenRect(new Rect(rect.xMin,             rect.yMin,             thickness,  rect.height), color);
            DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin,             thickness,  rect.height), color);
        }
    }
}
