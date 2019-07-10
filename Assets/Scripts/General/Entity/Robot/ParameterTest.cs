﻿using System;
using Simuro5v5;
using UnityEngine;
using System.IO;
using UnityEditor;

public class ParameterTest : MonoBehaviour
{
    public GameObject leftWheel, rightWheel;

    public float ForwardFactor;
    public float TorqueFactor;
    public float ForwardDrag;
    public float SidewayDrag;
    public float AngularDrag;
    public float ZeroAngularDrag;
    public float DoubleZeroDrag;

    public float LeftVelocity;
    public float RightVelocity;

    Vector3 forward_force, forward_drag, sideway_drag;
    Vector3 forward_left_drag, forward_right_drag;
    Vector3 torque, angular_drag;

    Collider Collider;
    Rigidbody rb;

    FileStream fs;
    StreamWriter writer;

    private Vector3 prevLeftPosition, prevRightPosition;

    void Start()
    {
        Collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = float.MaxValue;

        fs = File.Open(@"D:\Data\newplatform.csv", FileMode.OpenOrCreate);
        fs.SetLength(0);
        writer = new StreamWriter(fs);
        writer.WriteLine("x,y,r");

        OnPauseBtnClick();

        ForwardFactor = 102.89712678726376f;
        ForwardDrag = 79.81736047779975f;
        SidewayDrag = 950;

        TorqueFactor = 1156.1817018313f;
        AngularDrag = 3769.775104018879f;

        ZeroAngularDrag = 2097.9773f;
        DoubleZeroDrag = 760;

        prevLeftPosition = leftWheelPosition;
        prevRightPosition = rightWheelPosition;

        maxX = minX = transform.position.x;
        maxY = minY = transform.position.y;
    }

    private int tick;
    private int playTime;
    float prevEuler;

    void FixedUpdate()
    {
        tick++;
        if (tick % 2 == 0)
            return;

        playTime++;
        if (playTime < 30)
        {
            LeftVelocity = 125;
            RightVelocity = 125;
        }
        else if (playTime < 32)
        {
            LeftVelocity = -1;
            RightVelocity = 0;
        }
        else
        {
            LeftVelocity = -1;
            RightVelocity = 0;
        }

        if (playTime >= 29 && playTime <= 32)
        {
            Debug.Log($"{transform.eulerAngles.y - prevEuler}");
        }

        if (playTime == 33)
        {
            OnPauseBtnClick();
        }
        prevEuler = transform.eulerAngles.y;

        var forwardDir = transform.forward;

        var leftVelocity = leftWheelPosition - prevLeftPosition;
        var rightVelocity = rightWheelPosition - prevRightPosition;
        prevLeftPosition = leftWheelPosition;
        prevRightPosition = rightWheelPosition;

        if (LeftVelocity == 0 && RightVelocity == 0)
        {
            forward_drag = rb.velocity * -DoubleZeroDrag;
            rb.AddForce(forward_drag);
        }
        else if (LeftVelocity == 0)
        {
            var dot = Vector3.Dot(leftVelocity, forwardDir);
            if (Math.Abs(dot) > 0.001 && RightVelocity < 0)
            {
                var leftV = Vector3.Project(leftVelocity, forwardDir);
                if (dot > 0)
                {
                    forward_drag = rb.velocity * -DoubleZeroDrag;
                    rb.AddForce(forward_drag);
                    // 左手系
                    rb.AddTorque(leftV.magnitude * ZeroAngularDrag * Vector3.down, ForceMode.Impulse);
//                    rb.AddForceAtPosition(-ZeroAngularDrag * leftV,
//                        leftWheelPosition);
                }
                else if (dot < 0)
                {
                    forward_drag = rb.velocity * -DoubleZeroDrag;
                    rb.AddForce(forward_drag);
                    rb.AddTorque(leftV.magnitude * ZeroAngularDrag * Vector3.up, ForceMode.Impulse);
//                    rb.AddForceAtPosition(-ZeroAngularDrag * -leftV,
//                        leftWheelPosition);
                }
            }
        }
        else if (RightVelocity == 0)
        {
            var dot = Vector3.Dot(rightVelocity, forwardDir);
            if (Math.Abs(dot) > 0.001 && LeftVelocity < 0)
            {
                var rightV = Vector3.Project(rightVelocity, forwardDir);
                if (dot > 0)
                {
                    forward_drag = rb.velocity * -DoubleZeroDrag;
                    rb.AddForce(forward_drag);
                    rb.AddTorque(rightV.magnitude * ZeroAngularDrag * Vector3.up, ForceMode.Impulse);
//                    rb.AddForceAtPosition(-ZeroAngularDrag * rightV,
//                        rightWheelPosition);
                }
                else if (dot < 0)
                {
                    forward_drag = rb.velocity * -DoubleZeroDrag;
                    rb.AddForce(forward_drag);
                    rb.AddTorque(rightV.magnitude * ZeroAngularDrag * Vector3.down, ForceMode.Impulse);
//                    rb.AddForceAtPosition(-ZeroAngularDrag * -rightV,
//                        rightWheelPosition);
                }
            }
        }
        else
        {
            // 切向速度
            var sidewayV = Vector3.Project(rb.velocity, transform.right);
            // 切向阻力
//        sideway_drag = sidewayV.magnitude < 0.1 ? Vector3.zero : sidewayV / sidewayV.magnitude * -30000;
            sideway_drag = sidewayV * -SidewayDrag;
            rb.AddForce(sideway_drag);
        }

        // 动力
        forward_force = (LeftVelocity + RightVelocity) * ForwardFactor * forwardDir;
        // 速度方向的空气阻力
        forward_drag = rb.velocity * -ForwardDrag;
        rb.AddForce(forward_force + forward_drag);

        // 动力扭矩
        torque = (LeftVelocity - RightVelocity) * TorqueFactor * Vector3.up;
        // 阻力扭矩
        angular_drag = rb.angularVelocity * -AngularDrag;
        rb.AddTorque(torque + angular_drag);
        OutputData();
    }

    private Vector3 rightWheelPosition => transform.position + transform.right * Const.Robot.HRL;
    private Vector3 leftWheelPosition => transform.position - transform.right * Const.Robot.HRL;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            rb.AddForceAtPosition(rb.transform.forward * SidewayDrag, leftWheelPosition);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            rb.AddForceAtPosition(rb.transform.forward * SidewayDrag, leftWheelPosition);
        }
    }

    private void LateUpdate()
    {
        Debug.DrawRay(rightWheelPosition, forward_right_drag);
        Debug.DrawRay(leftWheelPosition, forward_left_drag);
    }

    float prevA = 0;
    private float prevZ = 0;
    float maxX, maxY, minX, minY;

    void OutputData()
    {
        var eulerAngles = rb.transform.eulerAngles;
        var av = string.Format("{0:N10}", eulerAngles.y - prevA);
        prevA = eulerAngles.y;

        var z = rb.transform.position.z;
        var v = string.Format("{0:N10}", z - prevZ);
        prevZ = z;

        var str = $"v {v}, av {av}";
//        Debug.Log(str);

        if (rb.transform.position.x > maxX)
            maxX = rb.transform.position.x;
        if (rb.transform.position.z > maxY)
            maxY = rb.transform.position.z;
        if (rb.transform.position.x < minX)
            minX = rb.transform.position.x;
        if (rb.transform.position.z < minY)
            minY = rb.transform.position.z;
//        Debug.Log($"{(maxX - minX + maxY - minY) / 2}");

        str = $"{rb.position.x},{rb.position.z},{rb.rotation.eulerAngles.y}";
        writer.WriteLine(str);
    }

    private void OnDestroy()
    {
        fs.Close();
    }

    void InitParameter()
    {
        rb.mass = Const.Robot.Mass;
        rb.drag = rb.angularDrag = 0;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY;
        rb.maxAngularVelocity = Const.Robot.maxAngularVelocity;

        ForwardFactor = Const.Robot.ForwardForceFactor;
        TorqueFactor = Const.Robot.TorqueFactor;
        ForwardDrag = Const.Robot.DragFactor;
        AngularDrag = Const.Robot.AngularDragFactor;
        DoubleZeroDrag = Const.Robot.DoubleZeroDragFactor;
    }

    public void OnResetBtnClick()
    {
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        rb.transform.position = Vector3.zero;
        rb.transform.rotation = Quaternion.Euler(0, 0, 0);
        prevA = 0;
        prevZ = 0;
        playTime = 0;
        maxX = minX = transform.position.x;
        maxY = minY = transform.position.z;
        Time.timeScale = 0;
    }

    public void OnPauseBtnClick()
    {
        if (Time.timeScale != 0)
            Time.timeScale = 0;
        else
            Time.timeScale = Const.DefaultTimeScale;
    }
}