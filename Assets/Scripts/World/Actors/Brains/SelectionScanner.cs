using System.Collections.Generic;
using UnityEngine;
using World.Actors.Player;

namespace World.Actors.Brains{
    /// Handles camera-based screen-to-world raycasting and marquee box intersection
    /// for character selection and selectable object inspection.
    public static class SelectionScanner{
        /// Raycasts from a screen position to detect a Character on the specified layer.
        public static Character RaycastCharacter(Camera cam, Vector2 screenPos, LayerMask layer, float maxDistance = 500f) =>
            Physics.Raycast(cam.ScreenPointToRay(screenPos), out RaycastHit hit, maxDistance, layer)
                ? hit.collider.GetComponentInParent<Character>()
                : null;

        /// Raycasts from a screen position to detect any ISelectable object on the specified layer.
        public static ISelectable RaycastSelectable(Camera cam, Vector2 screenPos, LayerMask layer, float maxDistance = 500f) =>
            Physics.Raycast(cam.ScreenPointToRay(screenPos), out RaycastHit hit, maxDistance, layer)
                ? hit.collider.GetComponentInParent<ISelectable>()
                : null;

        /// Gathers all characters from a candidate roster whose screen-projected positions fall within a 2D screen-space rectangle.
        public static List<Character> GetCharactersInScreenRect(Camera cam, Vector2 p1, Vector2 p2, List<Character> candidates){
            List<Character> enclosed      = new();
            Rect            selectionRect = GetScreenRect(p1, p2);

            foreach (Character character in candidates){
                Vector3 screenPoint = cam.WorldToScreenPoint(character.WorldPosition);
                if (screenPoint.z > 0 && selectionRect.Contains(new Vector2(screenPoint.x, screenPoint.y)))
                    enclosed.Add(character);
            }

            return enclosed;
        }

        /// Computes an axis-aligned screen-space rectangle from two opposing corner points.
        public static Rect GetScreenRect(Vector2 p1, Vector2 p2) =>
            Rect.MinMaxRect(
                Mathf.Min(p1.x, p2.x),
                Mathf.Min(p1.y, p2.y),
                Mathf.Max(p1.x, p2.x),
                Mathf.Max(p1.y, p2.y));

        /// Checks whether screen coordinates fall within the active display viewport.
        public static bool IsPointerInsideViewport(Vector2 screenPos) =>
            screenPos.x >= 0 && screenPos.x <= Screen.width && screenPos.y >= 0 && screenPos.y <= Screen.height;

        /// Renders a solid GUI color fill over the specified screen rectangle.
        public static void DrawScreenRect(Rect rect, Color color){
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// Renders an outline border around the specified screen rectangle with the given stroke thickness and color.
        public static void DrawScreenRectBorder(Rect rect, float thickness, Color color){
            DrawScreenRect(new Rect(rect.xMin,             rect.yMin,             rect.width, thickness),   color);
            DrawScreenRect(new Rect(rect.xMin,             rect.yMax - thickness, rect.width, thickness),   color);
            DrawScreenRect(new Rect(rect.xMin,             rect.yMin,             thickness,  rect.height), color);
            DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin,             thickness,  rect.height), color);
        }
    }
}
