using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using UnityEngine.Assertions;
using System;

public class Tuple<T1, T2>
{
  public T1 First { get; private set; }
  public T2 Second { get; private set; }
  internal Tuple(T1 first, T2 second)
  {
    First = first;
    Second = second;
  }
}

public class UMITVehicleController : ScriptComponent
{

  /// <summary>
  /// Speed for the steering hinge
  /// </summary>
  public float SteeringSpeed = 0.7f;

  /// <summary>
  /// Max force for the steering hinge
  /// </summary>
  public float SteeringForce = 6200000f;

  public KeyCode KeyLeft = KeyCode.LeftArrow;
  public KeyCode KeyRight = KeyCode.RightArrow;

  public KeyCode KeyUp = KeyCode.UpArrow;
  public KeyCode KeyDown = KeyCode.DownArrow;
  public KeyCode KeyReset = KeyCode.R;


  /// <summary>
  /// Stiffness of the Tires
  /// </summary>
  public float TireStiffness = 9000000;

  /// <summary>
  /// Damping coefficient of the tires
  /// </summary>
  public float TireDamping = 700000;



  private agx.RigidBody m_chassieRigidBody;
  private bool m_resetKeyDown = false;
  private agx.RigidBody m_moveBody = null;
  private agx.Constraint m_moveConstraint = null;

  bool m_motor_key_forward_down = false;
  bool m_motor_key_backward_down = false;

  private Dictionary<string, agx.Hinge> m_wheelHinges;

  private agx.Hinge m_steeringHinge;

  private List<agxModel.TwoBodyTire> m_tireModels;
  
  private float m_tireStiffness = -1.0f;
  private float m_tireDamping = -1.0f;

  private bool m_initialized = false;

  private const float AGX_EQUIVALENT_EPSILON = (float)1E-5;




  /// <summary>
  /// 
  /// </summary>
  class Vehicle
  {

    private agxSDK.Simulation m_simulation;
    private Dictionary<string, agx.Hinge> m_wheelHinges;
    private Brakes m_brakes;
    private agx.Timer m_clutchTimer;
    private const float m_clutchDuration = 1.0f;
    private bool m_clutchEngaged = false;
    private List<Tuple<double, double>> m_rpm_torque;
    private double[] m_brakeForceRange;
    private double[] m_gears;
    private double m_idleRPM;

    private void initVehicleParameters()
    {
      m_brakeForceRange = new double[] { -5E4, 5E4 };
      // RPM - Torque table for the engine
      m_rpm_torque = new List<Tuple<double, double>>
      {
        new Tuple<double, double>(100, 200 ),
        new Tuple<double, double>( 400, 1000 ),
        new Tuple<double, double>(600, 2500 ),
        new Tuple<double, double>(900, 3600 ),
        new Tuple<double, double>(1000, 3800 ),
        new Tuple<double, double>(1200, 3900 ),
        new Tuple<double, double>(1900, 4500 ),
        new Tuple<double, double>(2200, 4000 ),
        new Tuple<double, double>(3200, 500 ),
        new Tuple<double, double>(4200, 100 )
      };
      // Gears in the gearbox
      m_gears = new double[] { -200, 0, 100, 20 };
      m_idleRPM = 800;
    }

    public Vehicle(agxSDK.Simulation simulation, Dictionary<string, agx.Hinge> wheelHinges)
    {
      m_simulation = simulation;
      m_wheelHinges = wheelHinges;

      initVehicleParameters();

      initializeDrivetrain();

      Clutch.setEfficiency(1.0);
      Gearbox.setGear(2);

      m_brakes = new Brakes();

      // Initialize the brakes on the wheel hinges
      foreach (var h in m_wheelHinges)
      {
        h.Value.getLock1D().setEnable(false);
        m_brakes.Add(new Brake(h.Value, m_brakeForceRange));
      }
    }

    /// <summary>
    /// Engage the clutch so that the connection between the engine and the gear box is active
    /// </summary>
    public void engageClutch()
    {
      if (m_clutchEngaged)
        return;

      m_clutchTimer = new agx.Timer(true);
      m_clutch.setEfficiency(0.0);
    }

    /// <summary>
    /// Disengage the clutch so that the connection between the engine and the gear box is NOT active
    /// </summary>
    public void disengageClutch()
    {
      m_clutchEngaged = false;
      m_clutch.setEfficiency(0);
    }

    /// <summary>
    /// Animate the engagement of the clutch between 0..1 over some time
    /// </summary>
    public void update()
    {
      m_brakes.update();

      if (m_clutchTimer != null)
      {
        m_clutchEngaged = true;

        // Interpolate the clutch value from 0 to 1
        var now = m_clutchTimer.getTime();
        float t = (float)(now / 1000.0f / m_clutchDuration);
        var value = UnityEngine.Mathf.Lerp(0, 1, t);
        m_clutch.setEfficiency(value);
        Debug.Log(string.Format("Clutch {0}", value));
        if (t >= 1.0f)
          m_clutchTimer = null;
      }
    }

    public class Brake
    {
      private agx.Hinge m_hinge;
      public Brake(agx.Hinge hinge, double[] brakeForceRange)
      {
        // Initialize the brakes on the hinges
        m_hinge = hinge;
        m_hinge.getLock1D().setEnable(false);
        m_hinge.getLock1D().setForceRange(new agx.RangeReal(brakeForceRange[0], brakeForceRange[1]));
        m_hinge.getLock1D().setCompliance(1E-12);
        update();
      }

      /// <summary>
      /// Enable/Disable the brakes
      /// </summary>
      /// <param name="flag"></param>
      public void setEnable(bool flag)
      {
        m_hinge.getLock1D().setEnable(flag);
        m_hinge.getLock1D().setPosition(m_hinge.getAngle());
      }

      /// <summary>
      /// Update the position of the lock as long as the hinge is rotating fasther than a threshold
      /// If not, stop updating position and lock it to its current position
      /// </summary>
      public void update()
      {
        var speed = m_hinge.getCurrentSpeed();
        if (speed > 1E-3)
          m_hinge.getLock1D().setPosition(m_hinge.getAngle());
      }
    };

    /// <summary>
    /// Collect all brakes into one
    /// </summary>
    class Brakes
    {
      public Brakes()
      {
        m_brakes = new List<Brake>();
      }

      private List<Brake> m_brakes;

      public void Add(Brake brake)
      {
        m_brakes.Add(brake);
      }

      /// <summary>
      /// Enable all brakes
      /// </summary>
      /// <param name="flag"></param>
      public void setEnable(bool flag )
      {
        foreach (var b in m_brakes)
        {
          b.setEnable(flag);
        }
      }

      /// <summary>
      /// Update all brakes
      /// </summary>
      public void update()
      {
        foreach (var b in m_brakes)
          b.update();
      }
    }


    public agxDriveTrain.PidControlledEngine Engine
    {
      get { return m_engine; }
    }

    public agxDriveTrain.Clutch Clutch
    {
      get { return m_clutch; }
    }

    public agxDriveTrain.GearBox Gearbox
    {
      get { return m_gearbox; }
    }

    /// <summary>
    /// Put in reverse gear
    /// disable IDLE rpm on engin
    /// set the throttle in to some value 
    /// invoke the clutch
    /// </summary>
    public void reverse()
    {
      brake(false);
      Engine.setIdleRPM(double.NegativeInfinity);

      m_gearbox.setGear(0);
      Engine.setThrottle(0.7);
      engageClutch();
    }

    /// <summary>
    /// Enable/disable the brakes
    /// Put clutch into some intermediate value to "brake" the engine
    /// disengage the clutch
    /// Enable IDLE RPM
    /// No throttle
    /// </summary>
    /// <param name="flag"></param>
    public void brake(bool flag)
    {
      m_brakes.setEnable(flag);
      if (flag)
      { // Don't brake and drive with the engine at the same time
        disengageClutch();
        m_clutch.setEfficiency(0.2);
        Engine.setIdleRPM(m_idleRPM);
        Engine.setThrottle(0.0);
      }
    }

    /// <summary>
    /// Go forward:
    /// - Disable brake
    /// - Disable IDLE Rpm
    /// - Put in forward gear
    /// - Full throttle
    /// - Engage the clutch
    /// </summary>
    public void forward()
    {
      brake(false);
      Engine.setIdleRPM(double.NegativeInfinity);

      m_gearbox.setGear(2);
      Engine.setThrottle(1.0);
      engageClutch();
    }


    private agxDriveTrain.PidControlledEngine m_engine;
    private agxDriveTrain.Clutch m_clutch;
    private agxDriveTrain.GearBox m_gearbox;

    private void initializeDrivetrain()
    {
      // Create a drive train
      var driveTrain = new agxPowerLine.PowerLine();
      m_simulation.add(driveTrain);

      //  Create an engine.
      m_engine = new agxDriveTrain.PidControlledEngine();
      // Initialize the engine parameters
      initializeEngine(driveTrain, m_engine);

      //

      //                                    Engine
      //                                      ^
      //                                      |
      //                                      |
      //                                    Clutch
      //                                      ^
      //                                      |
      //                                      |
      //     FrontRightWheelActuator        GearBox           RearRightWheelActuator
      //          ^                           ^                         ^
      //          |                           |                         |
      //          |                           |                         |
      //   FrontDifferential < ------- CenterDifferential -----> RearDifferential
      //          |                                                     |
      //          |                                                     |
      //     FrontLeftWheelActuator                            RearLeftWheelActuator


      var engineShaft = new agxDriveTrain.Shaft();
      engineShaft.getRotationalDimension().setName("engineShaft");

      m_clutch = new agxDriveTrain.Clutch(0.0);
      var clutchShaft = new agxDriveTrain.Shaft();
      clutchShaft.getRotationalDimension().setName("clutchShaft");

      m_gearbox = new agxDriveTrain.GearBox();

      /// Initialize the gear box parameters
      initializeGearBox(m_gearbox);

      var gearBoxShaft = new agxDriveTrain.Shaft();
      gearBoxShaft.getRotationalDimension().setName("gearBoxShaft");

      bool status = false;
      status = m_engine.connect(agxPowerLine.UnitSide.UNIT_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT, engineShaft);
      Debug.Assert(status);
      status = m_clutch.connect(engineShaft, agxPowerLine.ConnectorSide.CONNECTOR_INPUT, agxPowerLine.UnitSide.UNIT_OUTPUT);
      Debug.Assert(status);
      status = m_clutch.connect(clutchShaft, agxPowerLine.ConnectorSide.CONNECTOR_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT);
      Debug.Assert(status);
      status = m_gearbox.connect(clutchShaft, agxPowerLine.ConnectorSide.CONNECTOR_INPUT, agxPowerLine.UnitSide.UNIT_OUTPUT);
      Debug.Assert(status);
      status = m_gearbox.connect(gearBoxShaft, agxPowerLine.ConnectorSide.CONNECTOR_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT);
      Debug.Assert(status);


      var centerDifferential = new agxDriveTrain.Differential();
      var frontDifferential = new agxDriveTrain.Differential();
      frontDifferential.setLock(true);
      var rearDifferential = new agxDriveTrain.Differential();
      rearDifferential.setLock(true);

      var frontDifferentialShaft = new agxDriveTrain.Shaft();
      frontDifferentialShaft.getRotationalDimension().setName("frontDifferentialShaft");
      var rearDifferentialShaft = new agxDriveTrain.Shaft();
      rearDifferentialShaft.getRotationalDimension().setName("rearDifferentialShaft");

      var frontRightWheelShaft = new agxDriveTrain.Shaft();
      var rearRightWheelShaft = new agxDriveTrain.Shaft();
      var frontLeftWheelShaft = new agxDriveTrain.Shaft();
      var rearLeftWheelShaft = new agxDriveTrain.Shaft();

      status = centerDifferential.connect(gearBoxShaft, agxPowerLine.ConnectorSide.CONNECTOR_INPUT, agxPowerLine.UnitSide.UNIT_OUTPUT);
      Debug.Assert(status);

      status = centerDifferential.connect(frontDifferentialShaft, agxPowerLine.ConnectorSide.CONNECTOR_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT);
      Debug.Assert(status);

      status = centerDifferential.connect(rearDifferentialShaft, agxPowerLine.ConnectorSide.CONNECTOR_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT);
      Debug.Assert(status);

      status = frontDifferential.connect(frontDifferentialShaft, agxPowerLine.ConnectorSide.CONNECTOR_INPUT, agxPowerLine.UnitSide.UNIT_OUTPUT);
      Debug.Assert(status);

      status = rearDifferential.connect(rearDifferentialShaft, agxPowerLine.ConnectorSide.CONNECTOR_INPUT, agxPowerLine.UnitSide.UNIT_OUTPUT);
      Debug.Assert(status);


      status = frontDifferential.connect(frontRightWheelShaft, agxPowerLine.ConnectorSide.CONNECTOR_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT);
      Debug.Assert(status);

      status = rearDifferential.connect(rearRightWheelShaft, agxPowerLine.ConnectorSide.CONNECTOR_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT);
      Debug.Assert(status);

      status = frontDifferential.connect(frontLeftWheelShaft, agxPowerLine.ConnectorSide.CONNECTOR_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT);
      Debug.Assert(status);

      status = rearDifferential.connect(rearLeftWheelShaft, agxPowerLine.ConnectorSide.CONNECTOR_OUTPUT, agxPowerLine.UnitSide.UNIT_INPUT);
      Debug.Assert(status);

      //  Now connect wheel shafts to constraints.
      var frontRightWheelActuator = new agxPowerLine.RotationalActuator(m_wheelHinges["RightFrontHinge"]);
      var frontLeftWheelActuator = new agxPowerLine.RotationalActuator(m_wheelHinges["LeftFrontHinge"]);
      var rearRightWheelActuator = new agxPowerLine.RotationalActuator(m_wheelHinges["RightRearHinge"]);
      var rearLeftWheelActuator = new agxPowerLine.RotationalActuator(m_wheelHinges["LeftRearHinge"]);

      status = frontRightWheelShaft.connect(frontRightWheelActuator);
      Debug.Assert(status);

      status = frontLeftWheelShaft.connect(frontLeftWheelActuator);
      Debug.Assert(status);

      status = rearRightWheelShaft.connect(rearRightWheelActuator);
      Debug.Assert(status);

      status = rearLeftWheelShaft.connect(rearLeftWheelActuator);
      Debug.Assert(status);
    }

    private void initializeGearBox(agxDriveTrain.GearBox gearBox)
    {
      agx.RealVector gears = new agx.RealVector();

      foreach(var g in m_gears)
        gears.Add(g);

      gearBox.setGearRatios(gears);
      gearBox.setGear(1);
    }

    private void initializeEngine(agxPowerLine.PowerLine drivetrain, agxDriveTrain.PidControlledEngine engine)
    {

      // set a value for the inertia of the rotational dimension
      engine.setInertia(0.4);
      drivetrain.setSource(engine);
      // Create a rpm/torque table


      var rpm_torque_table = new agx.RealPairVector();
      foreach (var t in m_rpm_torque)
        rpm_torque_table.Add(new agx.RealPair(t.First, t.Second));

      engine.setRPMTorqueTable(rpm_torque_table);
      engine.ignition(true);
      engine.setThrottle(1.0);
    }
  }

  Vehicle m_vehicle;

  UMITVehicleController()
  {
    m_wheelHinges = new Dictionary<string, agx.Hinge>();
  }


  bool equivalent(float lhs, float rhs, float epsilon = AGX_EQUIVALENT_EPSILON)
  {
    return (lhs + epsilon >= rhs) && (lhs - epsilon <= rhs);
  }

  private void updateTireModelParameters()
  {
    // Only update if changed
    if (equivalent(TireStiffness, m_tireStiffness) && equivalent(TireDamping, m_tireDamping))
      return;

    // If not using tire model, just skip this
    if (m_tireModels == null || m_tireModels.Count == 0)
      return;

    m_tireDamping = TireDamping;
    m_tireStiffness = TireStiffness;

    foreach (var m in m_tireModels)
    {
      m.setImplicitFrictionMultiplier(new agx.Vec2(1.2, 0.8));
      m.setStiffness(m_tireStiffness * 0.5, agxModel.TwoBodyTire.DeformationMode.RADIAL);
      m.setStiffness(m_tireStiffness, agxModel.TwoBodyTire.DeformationMode.LATERAL);
      m.setStiffness(m_tireStiffness * 0.5, agxModel.TwoBodyTire.DeformationMode.BENDING);
      m.setStiffness(m_tireStiffness * 0.1, agxModel.TwoBodyTire.DeformationMode.TORSIONAL);

      // Damping can be set in the same dimensions as m_tireStiffness. Again, we use a common multiplier

      // The unit for the translational damping coefficient is force * time/displacement (if using SI: Ns/m)
      // The unit for the rotational damping coefficient is torque * time/angular displacement (if using SI: Nms/rad)
      m.setDampingCoefficient(m_tireDamping * 2, agxModel.TwoBodyTire.DeformationMode.RADIAL);
      m.setDampingCoefficient(m_tireDamping, agxModel.TwoBodyTire.DeformationMode.LATERAL);
      m.setDampingCoefficient(m_tireDamping * 2, agxModel.TwoBodyTire.DeformationMode.BENDING);
      m.setDampingCoefficient(m_tireDamping * 10, agxModel.TwoBodyTire.DeformationMode.TORSIONAL);
    }
  }

  protected void inititializeTire(string name)
  {
    var sim = GetSimulation();
    Assert.IsNotNull(sim);

    var searchName = name + "Tire";
    var tire = sim.getRigidBody(searchName);
    Assert.IsNotNull(tire, "Unable to find RigidBody: " + searchName);

    searchName = name + "Rim";
    var rim = sim.getRigidBody(searchName);
    Assert.IsNotNull(rim, "Unable to find RigidBody: " + searchName);

    // For each tire/rim there is a LockJoint that we added in Momentum just to keep the two together.
    // We need to disable that constraint
    searchName = "Lock" + name;
    var lockJoint = sim.getLockJoint(searchName);
    Assert.IsNotNull(lockJoint, "Unable to find LockJoint: " + searchName);
    lockJoint.setEnable(false);

    // Make sure orientation of the wheel is correct
    var m = new agx.AffineMatrix4x4(new agx.Quat(new agx.Vec3(0, 0, 1),
                                                 new agx.Vec3(0, 1, 0)),
                                    new agx.Vec3());

    // Create a tire model that connects the Tire with the Rim
    var tireModel = new agxModel.TwoBodyTire(tire, 1.0, rim, 0.5, m);
    m_tireModels.Add(tireModel);
    sim.add(tireModel);
  }

  /// <summary>
  /// Go through each pair of tire/rim and create a TireModel
  /// </summary>
  protected void initializeTires()
  {
    m_tireModels = new List<agxModel.TwoBodyTire>();

    List<string> names = new List<string> { "RightRear", "LeftRear", "RightFront", "LeftFront" };

    foreach (var name in names)
    {
      inititializeTire(name);
    }

    updateTireModelParameters();

  }

  // Use this for initialization
  protected override bool Initialize()
  {
    return true;
  }

  void updateForceRange()
  {
    var steering_range = new agx.RangeReal(-SteeringForce, SteeringForce);
    m_steeringHinge.getMotor1D().setForceRange(steering_range);
  }

  /// <summary>
  /// Initialize the whole vehicle
  /// </summary>
  private void initializeVehicle()
  {
    if (m_initialized)
      return;

    var sim = GetSimulation();
    Assert.IsNotNull(sim);

    // Access the hinge used for steering
    m_steeringHinge = GetSimulation().getHinge("WaistHingeTurning");
    Assert.IsNotNull(m_steeringHinge);
     
    m_steeringHinge.getMotor1D().setEnable(true);
    m_steeringHinge.getMotor1D().setSpeed(0.0f);
    m_steeringHinge.getMotor1D().setLockedAtZeroSpeed(true);

    /// Access all the hinges attached to all the rims
    var hingeJointNames = new List<string> { "RightRearHinge", "LeftRearHinge", "RightFrontHinge", "LeftFrontHinge" };
    foreach (var n in hingeJointNames)
    {
      var hinge = sim.getHinge(n);
      Assert.IsNotNull(hinge, string.Format("Hinge {0} is missing in Simulation", n));

      m_wheelHinges.Add(hinge.getName(), hinge);
    }


    if (m_chassieRigidBody == null)
    {
      m_chassieRigidBody = sim.getRigidBody("FrontBody");
      Assert.IsNotNull(m_chassieRigidBody);
    }

    //Hinge.GetController<TargetSpeedController>().Enable = true;

    // Make sure body exists before I start using it.
    //Body.GetInitialized<RigidBody>().Native.setEnable(false);

    initializeTires();

    // Create the Vehicle class that initializes brakes etc.
    m_vehicle = new Vehicle(GetSimulation(), m_wheelHinges);

    m_initialized = true;
  }


  private void updateUI()
  {
    HudManager.Speed = m_chassieRigidBody.getVelocity().length();
    HudManager.EngineRPM = m_vehicle.Engine.getRPM();
  }

  // Update is called once per frame
  void Update()
  {
    // We need to initialize the vehicle here, to be sure we do have a valid
    // Native simulation with all the previously created objects in the simulation
    initializeVehicle();

    // Update the parameter models in case someone has changed the values
    updateTireModelParameters();

    // Update the HUD
    updateUI();

    // Update the vehicle (brakes/clutch etc.)
    m_vehicle.update();


    handleKeyboard();
  }

  private void handleKeyboard()
  {
    bool steering_key_down = false;

    // Handle keyboard
    if (Input.GetKey(KeyUp))
    {
      if (!m_motor_key_forward_down)
      {
        m_vehicle.forward();
      }

      m_motor_key_forward_down = true;
    }
    else
      m_motor_key_forward_down = false;

    if (Input.GetKey(KeyDown))
    {
      if (!m_motor_key_backward_down)
        m_vehicle.reverse();

      m_motor_key_backward_down = true;
    }
    else
      m_motor_key_backward_down = false;

    if (Input.GetKey(KeyLeft))
    {
      steering_key_down = true;
      updateForceRange();
      m_steeringHinge.getMotor1D().setSpeed(-SteeringSpeed);
    }

    if (Input.GetKey(KeyRight))
    {
      steering_key_down = true;
      updateForceRange();

      m_steeringHinge.getMotor1D().setSpeed(SteeringSpeed);
    }

    if (!steering_key_down)
      m_steeringHinge.getMotor1D().setSpeed(0);

    if (!m_motor_key_backward_down && !m_motor_key_forward_down)
      m_vehicle.brake(true);

    bool fast = Input.GetKey(KeyCode.LeftShift);
    if (m_motor_key_forward_down && fast)
        m_vehicle.Gearbox.setGear(3);

    if (Input.GetKey(KeyReset) && !m_resetKeyDown)
    {
      m_resetKeyDown = true;
      var pos = m_chassieRigidBody.getPosition();

      m_moveBody = new agx.RigidBody();
      GetSimulation().add(m_moveBody);

      m_moveBody.setMotionControl(agx.RigidBody.MotionControl.KINEMATICS);
      m_moveBody.setPosition(pos + new agx.Vec3(0, 2, 0));
      m_moveBody.setVelocity(0, 2, 0);

      var f1 = new agx.Frame();
      var f2 = new agx.Frame();
      agx.Constraint.calculateFramesFromWorld(pos, new agx.Vec3(0, 1, 0), m_moveBody, f1, m_chassieRigidBody, f2);
      m_moveConstraint = new agx.BallJoint(m_moveBody, f1, m_chassieRigidBody, f2);
      GetSimulation().add(m_moveConstraint);
    }
    else if (m_resetKeyDown)
    {
      GetSimulation().remove(m_moveBody);
      GetSimulation().remove(m_moveConstraint);
      m_resetKeyDown = false;
    }
  }
}
