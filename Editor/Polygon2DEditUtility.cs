/* Polygon2DUtility.cs
 * © Eddie Cameron 2019
 * ----------------------------
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Polygon2D {
    internal class Polygon2DEditUtility {
        const float k_HandlePointSnap = 10f;
        const float k_HandlePickDistance = 50f;

        private int m_MinPathPoints = 3;

        private int m_SelectedVertex = -1;
        private int m_SelectedEdgeVertex0 = -1;
        private int m_SelectedEdgeVertex1 = -1;
        private bool m_DeleteMode = false;

        private bool m_FirstOnSceneGUIAfterReset;

        private bool m_HandlePoint = false;
        private bool m_HandleEdge = false;

        public ScenePolygon EditingScenePoly { get; private set; }  // if set, edits will be local to this component
        public Polygon2D EditingPoly { get; private set; }

        public void Reset() {
            m_SelectedVertex = -1;
            m_SelectedEdgeVertex0 = -1;
            m_SelectedEdgeVertex1 = -1;
            m_FirstOnSceneGUIAfterReset = true;
            m_HandlePoint = false;
            m_HandleEdge = false;
        }

        private void UndoRedoPerformed() {
            var scenePoly = EditingScenePoly;
            StopEditing();
            StartEditing( scenePoly );
        }
        
        public void StartEditing( ScenePolygon scenePolygon ) {
            Polygon2D editingPoly;
            if ( scenePolygon?.Polygon == null )
                editingPoly = new Polygon2D();
            else {
                EditingScenePoly = scenePolygon;
                editingPoly = scenePolygon.Polygon;
            }

            StartEditing( editingPoly );
        }

        public void StartEditing( Polygon2D polygonProperty ) {
            Undo.undoRedoPerformed += UndoRedoPerformed;

            Reset();

            EditingPoly = polygonProperty;
            m_MinPathPoints = 3;
        }

        public void StopEditing() {
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            EditingScenePoly = null;
        }

        public bool OnSceneGUI() {
            Event evt = Event.current;
            m_DeleteMode = evt.command || evt.control;
            // Transform transform = m_ActiveCollider.transform;    TODO local polygons

            // Handles.Slider2D will render active point as yellow if there is keyboardControl set. We don't want that happening.
            GUIUtility.keyboardControl = 0;

            // HandleUtility.s_CustomPickDistance = k_HandlePickDistance;

            // Find mouse positions in local and world space
            Vector3 polyOrigin = EditingScenePoly ? EditingScenePoly.transform.position : Vector3.zero;
            Plane plane = new Plane( -Vector3.forward, polyOrigin );
            Ray mouseRay = HandleUtility.GUIPointToWorldRay( evt.mousePosition );
            float dist;
            plane.Raycast( mouseRay, out dist );

            Vector3 mouseWorldPos = mouseRay.GetPoint( dist );
            Vector2 mouseLocalPos = EditingScenePoly ? EditingScenePoly.WorldToPolygonSpace( mouseWorldPos ) : (Vector2)mouseWorldPos;

            // Select the active vertex and edge
            if ( evt.type == EventType.MouseMove || m_FirstOnSceneGUIAfterReset ) {
                float distance;
                m_SelectedVertex = EditingPoly.GetNearestVertex( mouseLocalPos, out distance );


                EditingPoly.GetNearestEdge( mouseLocalPos, out m_SelectedEdgeVertex0, out m_SelectedEdgeVertex1, out distance );

                if ( evt.type == EventType.MouseMove )
                    evt.Use();
            }

            // Do we handle point or line?
            if ( GUIUtility.hotControl == 0 ) {
                if ( m_SelectedEdgeVertex0 != m_SelectedEdgeVertex1 ) {
                    // Calculate snapping distance
                    Vector2 point = EditingPoly.GetVertex( m_SelectedVertex );
                    Vector3 worldPos = EditingScenePoly ? EditingScenePoly.PolygonToWorldSpace( point ) : (Vector3)point; // TODO transform.TransformPoint( point );
                    m_HandleEdge = ( HandleUtility.WorldToGUIPoint( worldPos ) - Event.current.mousePosition ).sqrMagnitude > k_HandlePointSnap * k_HandlePointSnap;
                    m_HandlePoint = !m_HandleEdge;
                }
                else {
                    m_HandleEdge = false;
                    m_HandlePoint = m_SelectedVertex >= 0;
                }

                if ( m_DeleteMode && m_HandleEdge ) {
                    m_HandleEdge = false;
                    m_HandlePoint = true;
                }
            }

            bool applyToCollider = false;

            // Edge handle
            if ( m_HandleEdge && !m_DeleteMode ) {
                Vector2 p0 = EditingPoly.GetVertex( m_SelectedEdgeVertex0 );
                Vector2 p1 = EditingPoly.GetVertex( m_SelectedEdgeVertex1 );
                Vector3 worldPosV0 = EditingScenePoly ? EditingScenePoly.PolygonToWorldSpace( p0 ) : (Vector3)p0;
                Vector3 worldPosV1 = EditingScenePoly ? EditingScenePoly.PolygonToWorldSpace( p1 ) : (Vector3)p1;

                Vector3 newPoint = MathUtils.NearestPointOnLine( worldPosV0, worldPosV1, mouseWorldPos );
                newPoint.z = polyOrigin.z; // transform.position.z;

                float guiSize = HandleUtility.GetHandleSize( newPoint );
                bool canDragEdge = ( HandleUtility.WorldToGUIPoint( newPoint ) - Event.current.mousePosition ).sqrMagnitude < guiSize * guiSize;
                if ( canDragEdge ) {

                    Handles.color = Color.green;
                    Handles.DrawAAPolyLine( 4.0f, new Vector3[] { worldPosV0, worldPosV1 } );
                    Handles.color = Color.white;

                    EditorGUI.BeginChangeCheck();
                    Handles.color = Color.green;

                    newPoint = Handles.Slider2D(
                        newPoint,
                        new Vector3( 0, 0, 1 ),
                        new Vector3( 1, 0, 0 ),
                        new Vector3( 0, 1, 0 ),
                        guiSize * .08f,
                        Handles.DotHandleCap,
                        Vector3.zero );
                    Handles.color = Color.white;
                    if ( EditorGUI.EndChangeCheck() ) {
                        EditingPoly.InsertVertex( m_SelectedEdgeVertex1, ( p0 + p1 ) / 2 );
                        m_SelectedVertex = m_SelectedEdgeVertex1;
                        m_HandleEdge = false;
                        m_HandlePoint = true;
                        applyToCollider = true;
                    }
                }
            }

            // Point handle
            if ( m_HandlePoint ) {
                Vector2 point = EditingPoly.GetVertex( m_SelectedVertex );
                Vector3 worldPos = EditingScenePoly ? EditingScenePoly.PolygonToWorldSpace( point ) : (Vector3)point;
                Vector2 screenPos = HandleUtility.WorldToGUIPoint( worldPos );

                float guiSize = HandleUtility.GetHandleSize( worldPos ) * 0.04f;

                if ( m_DeleteMode && evt.type == EventType.MouseDown &&
                    Vector2.Distance( screenPos, Event.current.mousePosition ) < k_HandlePickDistance ||
                    DeleteCommandEvent( evt ) ) {
                    if ( evt.type != EventType.ValidateCommand ) {
                        if ( EditingPoly.NumVertices > m_MinPathPoints ) {
                            EditingPoly.RemoveVertex( m_SelectedVertex );
                            Reset();
                            applyToCollider = true;
                        }
                    }
                    evt.Use();
                }

                EditorGUI.BeginChangeCheck();
                Handles.color = m_DeleteMode ? Color.red : Color.green;
                Vector3 newWorldPos = Handles.Slider2D(
                    worldPos,
                    new Vector3( 0, 0, 1 ),
                    new Vector3( 1, 0, 0 ),
                    new Vector3( 0, 1, 0 ),
                    guiSize,
                    Handles.DotHandleCap,
                    Vector3.zero );
                Handles.color = Color.white;
                if ( EditorGUI.EndChangeCheck() && !m_DeleteMode ) {
                    point = EditingScenePoly ? EditingScenePoly.WorldToPolygonSpace( newWorldPos ) : (Vector2)newWorldPos; //transform.InverseTransformPoint( newWorldPos );
                    //PolygonEditor.TestPointMove( m_SelectedPath, m_SelectedVertex, point, out m_LeftIntersect, out m_RightIntersect );
                    EditingPoly.SetVertex( m_SelectedVertex, point );
                    applyToCollider = true;
                }

                if ( !applyToCollider )
                    DrawEdgesForSelectedPoint( newWorldPos );
            }

            if ( DeleteCommandEvent( evt ) )
                Event.current.Use();  // If we don't use the delete event in all cases, it sceneview might delete the entire object

            m_FirstOnSceneGUIAfterReset = false;

            return applyToCollider;
        }

        private bool DeleteCommandEvent( Event evt ) {
            return ( evt.type == EventType.ExecuteCommand || evt.type == EventType.ValidateCommand ) && ( evt.commandName == "Delete" || evt.commandName == "SoftDelete" );
        }

        private void DrawEdgesForSelectedPoint( Vector3 worldPos ) {
            int pathPointCount = EditingPoly.NumVertices;
            int v0 = m_SelectedVertex - 1;
            if ( v0 == -1 ) {
                v0 = pathPointCount - 1;
            }
            int v1 = m_SelectedVertex + 1;
            if ( v1 == pathPointCount ) {
                v1 = 0;
            }

            Vector2 p0, p1;
            p0 = EditingPoly.GetVertex( v0 );
            p1 = EditingPoly.GetVertex( v1 );
            Vector3 worldPosV0 = EditingScenePoly ? EditingScenePoly.PolygonToWorldSpace( p0 ) : (Vector3)p0;
            Vector3 worldPosV1 = EditingScenePoly ? EditingScenePoly.PolygonToWorldSpace( p1 ) : (Vector3)p1;

            float lineWidth = 4.0f;
            Handles.color = m_DeleteMode ? Color.red : Color.green;
            Handles.DrawAAPolyLine( lineWidth, new Vector3[] { worldPos, worldPosV0 } );

            Handles.color = m_DeleteMode ? Color.red : Color.green;
            Handles.DrawAAPolyLine( lineWidth, new Vector3[] { worldPos, worldPosV1 } );
            Handles.color = Color.white;
        }
    }
}
