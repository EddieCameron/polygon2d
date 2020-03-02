/* ScenePolygon.cs
 * © Eddie Cameron 2019
 * ----------------------------
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Polygon2D
{
    public class ScenePolygon : MonoBehaviour
    {
        [SerializeField]
        private Polygon2D _polygon = new Polygon2D();
        public Polygon2D Polygon => _polygon;

        public void Reset() {
            _polygon = new Polygon2D(new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0) });
        }

        /// <summary>
        /// Make a polygon that is offset to this position, note that world z position is ignored
        /// </summary>
        /// <returns></returns>
        public Polygon2D GetWorldSpacePolygon()
        {
            var worldPoly = new Polygon2D(Polygon);
            for (int i = 0; i < worldPoly.NumVertices; i++)
            {
                worldPoly.SetVertex(i, PolygonToWorldSpace(worldPoly.GetVertex(i)));
            }
            return worldPoly;
        }

        public Vector2 WorldToPolygonSpace(Vector3 p)
        {
            return transform.InverseTransformPoint(p);
        }

        public Vector3 PolygonToWorldSpace(Vector2 p)
        {
            return transform.TransformPoint(p);
        }

        void OnDrawGizmosSelected()
        {
            GetWorldSpacePolygon().DrawGizmosInScene();
        }
    }
}
