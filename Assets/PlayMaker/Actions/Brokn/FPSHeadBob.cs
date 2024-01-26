using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{

	[ActionCategory("Brokn")]
	public class FPSHeadBob : FsmStateAction
	{
		public FsmGameObject HeadTransform;
		public FsmGameObject CameraTransform;
		public FsmFloat BobFrequency;
		public FsmFloat BobHorizontalAmplitude;
		public FsmFloat BobVerticalAmplitude;
		public FsmFloat HeadbobSmoothing;
		public FsmFloat IsWalking;
		public FsmFloat IsWalkingThreshHold;
			private float walkingtime;
			private Vector3 targetcameraposition;
		// Code that runs on entering the state.
		public override void OnUpdate()
		{
			if (IsWalking.Value < IsWalkingThreshHold.Value) walkingtime = 0;
            else { walkingtime += Time.deltaTime; }

			targetcameraposition = HeadTransform.Value.transform.position + CalculateHeadBobOffset(walkingtime);
			CameraTransform.Value.transform.position = Vector3.Lerp(CameraTransform.Value.transform.position, targetcameraposition, HeadbobSmoothing.Value);
			if ((CameraTransform.Value.transform.position - targetcameraposition).magnitude <= .001) CameraTransform.Value.transform.position = targetcameraposition;

		}

		public Vector3 CalculateHeadBobOffset(float t)
		{
			float horizontaloffset = 0;
			float verticaloffset = 0;
			Vector3 offset = Vector3.zero;
			if (t > 0)
			{
				horizontaloffset = Mathf.Cos(t * BobFrequency.Value) * BobHorizontalAmplitude.Value;
				verticaloffset = Mathf.Sin(t * BobFrequency.Value * 2) * BobVerticalAmplitude.Value;
				offset = HeadTransform.Value.transform.right * horizontaloffset + HeadTransform.Value.transform.up * verticaloffset;

			}
			return offset;
			

        }
	}


}
