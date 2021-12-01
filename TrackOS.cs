//Script made by Lelebees: Last update at 18-10-2021 10:15 UTC+2 (Central European Summer Time)
//TrackOS V1.2 (Not backwards compatible)

//Tweakable variables below here
string antennaName = "Antenna";
string broadcastChannel = "channel 1"; //you can set this to literally anything, and I advise you to do so, to keep out people who are guessing lucky.
bool receiverSystem = false; //set to true if you want to receive a programmable block's location.
string outputPanelName = "Output LCD";
int screenNumber = 0; //this is the integer that use can use to decide which cockpit/PB screen (or whatever) you want to use. No guarantee this works on modded blocks!

//reciever only variables
bool policeSender = true; //receiver system only. Recommended false when using multiple Senders on one channel.
long allowedProgBlockID = 1234567890; //This is the ID of the programmable block you are receiving from.

//sender only variables
long receiverProgBlockID = 1234567890; //This is the ID of the programmable block you are sending to. Use this to immedeately trigger a send when the Receiver is in range
bool checkingRange = true; // The on off switch for the above mentioned feature.
bool useInternalTimer = true; //choose if you want the internal timer to work, or if you want to use your own timers
int timeDelay = 1800; //delay between send commands in ticks (60 ticks = 1 second) (Send Systems only)


//Do NOT touch the variables below this line. This WILL break the program.
bool sentInrangeMessage = false;
int messageCount = 0;
int tickCount = 0;
IMyRadioAntenna antenna;
IMyTerminalBlock statusDisplay;
IMyTextSurface textSurface;
IEnumerator<bool> _stateMachine;

public Program() //This is the setup, which is automatically ran every time script is compiled or after a world reload
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    //assign blocks to variables
    antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
    statusDisplay = GridTerminalSystem.GetBlockWithName(outputPanelName);
    //Decide what type of TextSurface the display is.
    if (statusDisplay is IMyTextSurface)
    {
        textSurface = (IMyTextSurface)statusDisplay;
    }
    else if (statusDisplay is IMyTextSurfaceProvider)
    {
        textSurface = ((IMyTextSurfaceProvider)statusDisplay).GetSurface(screenNumber);
    }
    else
    {
        textSurface = ((IMyTextSurfaceProvider)Me).GetSurface(screenNumber);
    }
    Echo("Setup complete. System  messages will now appear on displays.\n");
    textSurface.WriteText("Setup complete.\n", false);

    if (receiverSystem == false)
    {
        _stateMachine = Send(broadcastChannel);
        Runtime.UpdateFrequency |= UpdateFrequency.Once;
        Echo ("Sending test complete. System should be ready to send coordinates.");
    }
}

public void Main(string arg, UpdateType updateType) 
{
    // Usually I verify that the argument is empty or a predefined value before running the state
    // machine. This way we can use arguments to control the script without disturbing the
    // state machine and its timing. For the purpose of this example however, I will omit this.

    // We only want to run the state machine(s) when the update type includes the
    // "Once" flag, to avoid running it more often than it should. It shouldn't run
    // on any other trigger. This way we can combine state machine running with
    // other kinds of execution, like tool bar commands, sensors or what have you.
    if ((updateType & UpdateType.Once) == UpdateType.Once) 
    {
        RunStateMachine();
    }
    else
    {
        tickCount++;
        arg = arg.ToLower();
        switch(arg) 
        { 
            case "send":
                _stateMachine = Send(broadcastChannel); //this is where we set messageText
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
            //RunStateMachine();
                break;
            
            case "get id":
                GetID();
                break;    
            
            default:
                Loop();
                break;
        }
    }
}

// ***MARKER: Coroutine Execution
public void RunStateMachine()
{
    // If there is an active state machine, run its next instruction set.
    if (_stateMachine != null) 
    {
        // The MoveNext method is the most important part of this system. When you call
        // MoveNext, your method is invoked until it hits a `yield return` statement.
        // Once that happens, your method is halted and flow control returns _here_.
        // At this point, MoveNext will return `true` since there's more code in your
        // method to execute. Once your method reaches its end and there are no more
        // yields, MoveNext will return false to signal that the method has completed.
        // The actual return value of your yields are unimportant to the actual state
        // machine.
        bool hasMoreSteps = _stateMachine.MoveNext();

        // If there are no more instructions, we stop and release the state machine.
        if (hasMoreSteps)
        {
            // The state machine still has more work to do, so signal another run again, 
            // just like at the beginning.
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        } 
        else 
        {
            _stateMachine.Dispose();

            // In our case we just want to run this once, so we set the state machine
            // variable to null. But if we wanted to continously run the same method, we
            // could as well do
            // _stateMachine = RunStuffOverTime();
            // instead.
            _stateMachine = null;
        }
    }
}

// ***MARKER: Coroutine Example
// The return value (bool in this case) is not important for this example. It is not
// actually in use.
public IEnumerator<bool> Send(string broadcastChannel) 
{

    // Then we will tell the script to stop execution here and let the game do it's
    // thing. The time until the code continues on the next line after this yield return
    // depends  on your State Machine Execution and the timer setup.
    // The `true` portion is there simply because an enumerator needs to return a value
    // per item, in our case the value simply has no meaning at all. You _could_ utilize
    // it for a more advanced scheduler if you want, but that is beyond the scope of this
    // tutorial.

    //pepare for sending
    antenna.EnableBroadcasting = true;
    messageCount++;
    Vector3D Position = Me.GetPosition();
    string messageText = $"GPS:Tracker Location {messageCount}:{Position.X}:{Position.Y}:{Position.Z}:#FF0000:";
    Echo ("DEBUG A");

    yield return true; //wait* a tick 
    yield return true;
    
    //Send the message
    IGC.SendBroadcastMessage(broadcastChannel, messageText, TransmissionDistance.TransmissionDistanceMax);  
	textSurface.WriteText ("sent message ["+messageCount+"] succesfully.\n", true);
    
    //custom actions box (When something has been sent)
    Echo("DEBUG B");
    //end of custom actions box
    
    //prepare for ending the send thing
    yield return true; //wait* a tick
    yield return true; // wait another tick so this fucking thing actually sends
    
    // the following comment has been left in to illustrate my struggles with the program, it is no longer relevant
    //okay so that didnt "Work" either. It does get to all this shite but no send. GO 9 YIELDS

    antenna.EnableBroadcasting = false;
    Echo("DEBUG C");
    
    // *The program doesn't actually wait. It stops and runs from that point on the next tick. this lowers the instruction count, making the script more preformance friendly, however, we're using it to make a delay in between certain actions
    //IT WORKS, WHAHAHAH YES IT WORKS! FUCK YOU COROUTINES, I AM A GOD OF SCRIPTING! TREMBLE IN FEAR BEFORE THE GREAT AND MIGHTY LELEBEES!
}

public void Receive(string broadcastChannel) //IGC magik
{
    IGC.RegisterBroadcastListener(broadcastChannel);
    List<IMyBroadcastListener> listenerList = new List<IMyBroadcastListener>();
    IGC.GetBroadcastListeners(listenerList);
    if (listenerList[0].HasPendingMessage) //if there is a message in the queue
    {
        MyIGCMessage receivedMessage = new MyIGCMessage();
        receivedMessage = listenerList[0].AcceptMessage();
        string receivedText = receivedMessage.Data.ToString(); //take the message text
        string receivedChannel = receivedMessage.Tag; //take the message tag (channel)
        long messageSender = receivedMessage.Source; //take the sender ID

        //Do something with the information!
        if (policeSender == true)
        {
            if (messageSender == allowedProgBlockID)
            {
                textSurface.WriteText("Message received on channel "+receivedChannel+"\n", true);
                textSurface.WriteText("Message content: "+receivedText+"\n", true);
                //add custom actions here if policeSender is on and it is the correct sender

                //end of custom actions box
            }
            else
            {
                textSurface.WriteText("Recieved message from unlisted reciever "+messageSender.ToString()+" on channel "+receivedChannel+"\n", true);
                //custom actions when incorrect sender received

                //end of custom actions box
            }
        }
        else
        {
            textSurface.WriteText("Message received on channel "+receivedChannel+"\n", true);
            textSurface.WriteText("Message content: "+receivedText+"\n", true);
            //add custom actions here if policeSender is off

            //end of custom actions box
        }
    }
}

public void GetID()
{
    long myID = IGC.Me;
    textSurface.WriteText(myID.ToString()+"\n", true);
    Me.CustomData = myID.ToString();
    textSurface.WriteText("ID saved to CustomData\n", true);
}

public void Loop()
{
    if(receiverSystem)
    {
        Receive(broadcastChannel);
    }
    else if(checkingRange)
    {
        bool endpoint = IGC.IsEndpointReachable(receiverProgBlockID, TransmissionDistance.TransmissionDistanceMax);
        
        if(endpoint && sentInrangeMessage == false)
        {
            //this will immedeately trigger the send command so location is broadcasted.
            _stateMachine = Send(broadcastChannel);
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
            sentInrangeMessage = true;
            textSurface.WriteText("Found Receiver in range! Sent co-ordinates immedeately!\n", false);
            //custom actions box when a reciever has been found in range

            //end of custom actions box
        }
        else if(!endpoint)
        {
            sentInrangeMessage = false;
        }
    }
    if (!receiverSystem & tickCount == timeDelay & useInternalTimer == true)
    {
        tickCount = 0;
        _stateMachine = Send(broadcastChannel);
        Runtime.UpdateFrequency |= UpdateFrequency.Once;
    }
}

//Virtually Invisible Communication Protocol : VICP (looks a lot like VOIP)