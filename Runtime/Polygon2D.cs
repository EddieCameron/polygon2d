using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Polygon2D {
    [Serializable]
    public class Polygon2D {
        [SerializeField]
        List<Vector2> vertices;

        public int NumVertices => vertices.Count;

        Rect? _boundsCache = null;
        public Rect Bounds {
            get {
                if ( !_boundsCache.HasValue ) {
                    _boundsCache = new Rect( this.vertices[0], Vector2.zero );
                    for ( int i = 1; i < NumVertices; i++ ) {
                        _boundsCache = Bounds.Encapsulate( this.vertices[i] );
                    }
                }
                return _boundsCache.Value;
            }
        }

        public bool IsValid => NumVertices >= 3;

        public Polygon2D() {
            this.vertices = new List<Vector2>();
        }

        public Polygon2D( ICollection<Vector2> vertices ) {
            if ( vertices.Count < 3 )
                throw new InvalidOperationException( "Need at least 3 vertices to make a polygon" );

            this.vertices = new List<Vector2>( vertices );
        }
        public Polygon2D( Polygon2D other ) {
            this.vertices = new List<Vector2>( other.vertices );
        }

        public Polygon2D Clone() {
            return new Polygon2D( this );
        }

        public void Translate( Vector2 translation ) {
            for ( int i = 0; i < NumVertices; i++ ) {
                vertices[i] += translation;
            }
            SetDirty();
        }

        public void Rotate( float radiansAnticlockwise ) {
            float sin = Mathf.Sin( radiansAnticlockwise );
            float cos = Mathf.Cos( radiansAnticlockwise );
            for ( int i = 0; i < NumVertices; i++ ) {
                var p = vertices[i];
                Vector2 q;
                q.x = p.x * cos - p.y * sin;
                q.y = p.x * sin + p.y * cos;
                vertices[i] = q;
            }
            SetDirty();
        }
        
        public void Scale( Vector2 scale ) {
            for ( int i = 0; i < NumVertices; i++ ) {
                vertices[i] *= scale;
            }
            SetDirty();
        }

        public void SetVertex( int atIdx, Vector2 point ) {
            vertices[atIdx] = point;
            SetDirty();
        }

        public void InsertVertex( int atIdx, Vector2 point ) {
            vertices.Insert( atIdx, point );
            SetDirty();
        }

        public void RemoveVertex( int atIdx ) {
            vertices.RemoveAt( atIdx );
            SetDirty();
        }

        public Vector2 GetVertex( int idx ) {
            return vertices[idx];
        }

        public bool Contains( Vector2 point ) {
            // first just check bounding box
            if ( !Bounds.Contains( point ) )
                return false;

            return GetWindingNumber( point ) != 0;
        }

        /// <summary>
        /// How many times does the shape "wind" around this point
        /// - 0 means point is outside shape
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int GetWindingNumber( Vector2 point ) {
            //http://geomalgorithms.com/a03-_inclusion.html
            /*
            wn_PnPoly( Point P, Point V[], int n )
            {
                int    wn = 0;    // the  winding number counter

                // loop through all edges of the polygon
                for (each edge E[i]:V[i]V[i+1] of the polygon) {
                    if (E[i] crosses upward ala Rule #1)  {
                        if (P is  strictly left of E[i])    // Rule #4
                            ++wn;   // a valid up intersect right of P.x
                    }
                    else
                    if (E[i] crosses downward ala Rule  #2) {
                        if (P is  strictly right of E[i])   // Rule #4
                            --wn;   // a valid down intersect right of P.x
                    }
                }
                return wn;    // =0 <=> P is outside the polygon

            }
            */

            int windingNumber = 0;

            for ( int i = 0; i < NumVertices; i++ ) {
                var e0 = vertices[i];
                var e1 = vertices[( i + 1 ) % NumVertices];   // TODO precalc edges?
                if ( e0.y <= point.y ) {
                    if ( e1.y > point.y &&      // an upward crossing
                    IsLeft( e0, e1, point ) > 0 ) { // on left
                        windingNumber++;
                    }
                }
                else if ( e1.y <= point.y &&    // a downward crossing
                    IsLeft( e0, e1, point ) < 0 ) { // on right
                    windingNumber--;
                }
            }
            return windingNumber;
        }

        /// <summary>
        /// Get a random point inside this polygon
        /// </summary>
        /// <returns></returns>
        public Vector2 RandomPointWithin() {
            // brute force
            Vector2 point = Vector2.zero;
            int safetyCheck = 1000;
            do {
                point = Bounds.RandomPointWithin();
                safetyCheck--;
            }
            while ( safetyCheck > 0 && !Contains( point ) );

            return point;
        }

        /// <summary>
        /// Test whether the given line segment intersects with any of the polygon edges
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool DoesIntersectWithLine( Vector2 a, Vector2 b ) {
            for ( int i = 0; i < NumVertices; i++ ) {
                var e0 = vertices[i];
                var e1 = vertices[( i + 1 ) % NumVertices];   // TODO precalc edges?
                if ( MathUtils.TestLineIntersection( a, b, e0, e1 ) )
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get the closest vertex in this polygon to the given point.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="vertexIdx"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public int GetNearestVertex( Vector2 p, out float distance ) {
            distance = float.MaxValue;
            int closestIdx = -1;
            for ( int i = 0; i < NumVertices; i++ ) {
                float dist = ( p - vertices[i] ).sqrMagnitude;
                if ( i == 0 || dist < distance ) {
                    distance = dist;
                    closestIdx = i;
                }
            }

            if ( closestIdx >= 0 ) {
                distance = Mathf.Sqrt( distance );
            }
            return closestIdx;
        }

        /// <summary>
        /// Get the cclosest edge (from edgeVertexA to edgeVertexB) in this polygon to the given point
        /// </summary>
        /// <param name="p"></param>
        /// <param name="edgeVertexA"></param>
        /// <param name="edgeVertexB"></param>
        /// <param name="distance"></param>
        public void GetNearestEdge( Vector2 p, out int edgeVertexA, out int edgeVertexB, out float distance ) {
            if ( NumVertices == 0 ) {
                edgeVertexA = edgeVertexB = -1;
                distance = 0;
                return;
            }
            if ( NumVertices == 1 ) {
                edgeVertexA = 0;
                edgeVertexB = 0;
                distance = ( p - vertices[0] ).magnitude;
                return;
            }


            distance = float.MaxValue;
            edgeVertexA = -1;
            for ( int i = 0; i < NumVertices; i++ ) {
                Vector2 p0 = vertices[i];
                Vector2 p1 = vertices[( i + 1 ) % NumVertices];
                float dist = MathUtils.DistanceFromPointToLine( p0, p1, p );
                if ( i == 0 || dist < distance ) {
                    distance = dist;
                    edgeVertexA = i;
                }
            }

            edgeVertexB = ( edgeVertexA + 1 ) % NumVertices;
        }

        /// <summary>
        /// Is a point to the left, right, or on, an infinite line passing between two points
        /// > 0 : left of line
        /// < 0 : right of line
        /// == 0 : on line
        /// </summary>
        /// <returns></returns>
        static float IsLeft( Vector2 p0, Vector2 p1, Vector2 pTest ) {
            return ( ( p1.x - p0.x ) * ( pTest.y - p0.y )
                    - ( pTest.x - p0.x ) * ( p1.y - p0.y ) );
        }

        /// <summary>
        /// Mark lazy calculated values as needing to be refreshed
        /// </summary>
        void SetDirty() {
            _boundsCache = null;
        }

        #if UNITY_EDITOR
        public void DrawGizmosInScene() {
            Color c = Gizmos.color;
            Gizmos.color = Color.cyan;
            for ( int i = 0; i < NumVertices; i++ ) {
                Vector2 p0 = vertices[i];
                Vector2 p1 = vertices[( i + 1 ) % NumVertices];
                Gizmos.DrawLine( p0, p1 );
            }
            Gizmos.color = c;
        }
        #endif
    }
}
