using com.zibraai.smoke_and_fire.Solver;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Manipulators
{
    public class ManipulatorCompare : Comparer<Manipulator>
    {
        // Compares manipulator type ID
        public override int Compare(Manipulator x, Manipulator y)
        {
            int result = x.GetManipulatorType().CompareTo(y.GetManipulatorType());
            if (result != 0)
            {
                return result;
            }
            if (x.enabled != y.enabled)
            {
                return y.enabled.CompareTo(x.enabled);
            }
            else if (x is ZibraSmokeAndFireForceField)    //We assume x and y have same type here
            {
                var xSDF = x.GetComponentInParent<SDFObjects.SDFObject>();
                var ySDF = y.GetComponentInParent<SDFObjects.SDFObject>();
                return ySDF.GetSDFType().CompareTo(xSDF.GetSDFType());
            }
            else
            {
                return x.GetHashCode().CompareTo(y.GetHashCode());
            }
        }
    }

    [ExecuteInEditMode]
    abstract public class Manipulator : MonoBehaviour
    {
#if UNITY_EDITOR
        // Used to update editors
        public event Action onChanged;
        public void NotifyChange()
        {
            if (onChanged != null)
            {
                onChanged.Invoke();
            }
        }
#endif

        [SerializeField]
        [HideInInspector]
        public float[] ConstAdditionalData = new float[0]; // Data to send to a compute buffer once

        [HideInInspector]
        public bool isInitialized = false;

        [SerializeField]
        [HideInInspector]
        public Vector4 AdditionalData0;

        [SerializeField]
        [HideInInspector]
        public Vector4 AdditionalData1;

        public static readonly List<Manipulator> AllManipulators = new List<Manipulator>();

        [NonSerialized]
        [HideInInspector]
        public Matrix4x4 PreviousTransform;

        public enum ManipulatorType
        {
            None,
            Emitter,
            Void,
            ForceField,
            AnalyticCollider,
            NeuralCollider,
            GroupCollider,
            Detector,
            SpeciesModifier,
            EffectParticleEmitter,
            TypeNum
        }

        protected void OnEnable()
        {
            if (!AllManipulators?.Contains(this) ?? false)
            {
                AllManipulators.Add(this);
            }
        }

        protected void OnDisable()
        {
            if (AllManipulators?.Contains(this) ?? false)
            {
                AllManipulators.Remove(this);
            }
        }

        private void Update()
        {
            PreviousTransform = transform.localToWorldMatrix;
        }

        private void Start()
        {
            PreviousTransform = transform.localToWorldMatrix;
        }

        public virtual void InitializeConstData()
        {
        }

        virtual public Matrix4x4 GetTransform()
        {
            return transform.localToWorldMatrix;
        }

        virtual public Quaternion GetRotation()
        {
            return transform.rotation;
        }

        virtual public Vector3 GetPosition()
        {
            return transform.position;
        }

        virtual public Vector3 GetScale()
        {
            return transform.lossyScale;
        }

        virtual public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.None;
        }

#if UNITY_EDITOR
        protected void OnDestroy()
        {
            foreach (var instance in ZibraSmokeAndFire.AllInstances)
            {
                if (instance != null)
                {
                    instance.RemoveManipulator(this);
                }
            }
        }
#endif
    }
}
