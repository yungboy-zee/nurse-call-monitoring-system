/*

  INPUTS  (call buttons):
    Pin 2  — Room 101, Ward A  [Hardware Interrupt INT0]
    Pin 3  — Room 102, Ward A  [Hardware Interrupt INT1]
    Pin 4  — Room 201, Ward B  [Polled, 20ms debounce]
    Pin 5  — Room 202, Ward B  [Polled, 20ms debounce]
    Pin 6  — ICU Bay 1, ICU    [Polled, 20ms debounce]
    Pin 7  — ICU Bay 2, ICU    [Polled, 20ms debounce]

  OUTPUTS (at nurses station):
    Pin 8  — Buzzer (active buzzer, positive terminal)
              Buzzes for BUZZER_DURATION_MS on each new alert
              Automatically silences after the set duration

    Pin 9  — Alert LED (red LED + 220Ω resistor to GND)
              ON whenever any unresolved emergency is active
              OFF only when WinForms app sends "CLEAR" over serial

  SERIAL COMMUNICATION:
    Baud rate : 9600
    TX format : ALERT,<Room>,<Ward>\r\n  (Arduino → PC)
    RX format : CLEAR\r\n               (PC → Arduino, when all resolved)

  WIRING:
    Each button  : Pin → Button → GND  (INPUT_PULLUP, no resistor needed)
    Buzzer (+)   : Pin 8 → Buzzer(+), Buzzer(-) → GND
    LED          : Pin 9 → 220Ω resistor → LED(+), LED(-) → GND
*/

// Pin Assignments
const int BUZZER_PIN = 8;
const int LED_PIN    = 9;

// Timing Constants 
const unsigned long DEBOUNCE_MS       = 20;   // ms — stable LOW before firing
const unsigned long COOLDOWN_MS       = 300;  // ms — min gap between alerts per button
const unsigned long BUZZER_DURATION_MS= 2000; // ms — how long buzzer sounds per alert

// Button Structure
struct CallButton {
  int          pin;
  const char*  room;
  const char*  ward;
  bool         lastStable;
  bool         alertSent;
  unsigned long firstLowTime;
  bool         timing;
};

CallButton BUTTONS[] = {
  { 2, "Room 101", "Ward A", HIGH, false, 0, false },
  { 3, "Room 102", "Ward A", HIGH, false, 0, false },
  { 4, "Room 201", "Ward B", HIGH, false, 0, false },
  { 5, "Room 202", "Ward B", HIGH, false, 0, false },
  { 6, "ICU Bay 1", "ICU",   HIGH, false, 0, false },
  { 7, "ICU Bay 2", "ICU",   HIGH, false, 0, false },
};

const int NUM_BUTTONS = sizeof(BUTTONS) / sizeof(BUTTONS[0]);

// State Variables
volatile bool isr2Triggered  = false;
volatile bool isr3Triggered  = false;

unsigned long lastAlertTime[6]  = { 0 };

bool          buzzerActive      = false;
unsigned long buzzerStartTime   = 0;

bool          ledOn             = false;    // tracks LED state
int           activeAlertCount  = 0;        // how many unresolved alerts exist

// Interrupt Service Routines  
void isr_pin2() { isr2Triggered = true; }
void isr_pin3() { isr3Triggered = true; }


void setup() {
  Serial.begin(9600);

  // Configure button input pins
  for (int i = 0; i < NUM_BUTTONS; i++) {
    pinMode(BUTTONS[i].pin, INPUT_PULLUP);
  }

  // Configure output pins
  pinMode(BUZZER_PIN, OUTPUT);
  pinMode(LED_PIN,    OUTPUT);
  digitalWrite(BUZZER_PIN, LOW);   // buzzer off at startup
  digitalWrite(LED_PIN,    LOW);   // LED off at startup

  // Attach hardware interrupts
  attachInterrupt(digitalPinToInterrupt(2), isr_pin2, FALLING);
  attachInterrupt(digitalPinToInterrupt(3), isr_pin3, FALLING);

  Serial.println("READY,NurseCallSystem,v4.0");
}


void loop() {
  unsigned long now = millis();

  // 1. Handle hardware interrupt flags (pins 2 & 3)
  if (isr2Triggered) { isr2Triggered = false; handleInstantPress(0, now); }
  if (isr3Triggered) { isr3Triggered = false; handleInstantPress(1, now); }

  // 2. Poll remaining buttons (pins 4–7)
  for (int i = 2; i < NUM_BUTTONS; i++) {
    handlePolledPress(i, now);
  }

  // 3. Check if buzzer duration has expired — silence it automatically
  if (buzzerActive && (now - buzzerStartTime >= BUZZER_DURATION_MS)) {
    digitalWrite(BUZZER_PIN, LOW);
    buzzerActive = false;
  }

  // 4. Listen for CLEAR command from WinForms app
  checkSerialInput();
}

//  Handle Interrupt-Triggered Button (pins 2 & 3) 
void handleInstantPress(int idx, unsigned long now) {
  if ((now - lastAlertTime[idx]) < COOLDOWN_MS) return;
  if (digitalRead(BUTTONS[idx].pin) != LOW)      return;

  triggerAlert(idx, now);
}

//  Handle Polled Button (pins 4) 
void handlePolledPress(int idx, unsigned long now) {
  bool reading = digitalRead(BUTTONS[idx].pin);

  if (reading == LOW) {
    if (!BUTTONS[idx].timing && !BUTTONS[idx].alertSent) {
      BUTTONS[idx].firstLowTime = now;
      BUTTONS[idx].timing       = true;
    }
    if (BUTTONS[idx].timing && !BUTTONS[idx].alertSent) {
      if ((now - BUTTONS[idx].firstLowTime) >= DEBOUNCE_MS) {
        if ((now - lastAlertTime[idx]) >= COOLDOWN_MS) {
          triggerAlert(idx, now);
          BUTTONS[idx].alertSent = true;
          BUTTONS[idx].timing    = false;
        }
      }
    }
  } else {
    BUTTONS[idx].timing    = false;
    BUTTONS[idx].alertSent = false;
  }
}

//  Trigger Alert — sends serial, activates buzzer and LED 
void triggerAlert(int idx, unsigned long now) {
  // Send alert string to WinForms app
  Serial.print("ALERT,");
  Serial.print(BUTTONS[idx].room);
  Serial.print(",");
  Serial.println(BUTTONS[idx].ward);

  lastAlertTime[idx] = now;
  activeAlertCount++;

  // Turn LED ON (stays on until all alerts resolved)
  digitalWrite(LED_PIN, HIGH);
  ledOn = true;

  // Start buzzer — it will self-silence after BUZZER_DURATION_MS
  digitalWrite(BUZZER_PIN, HIGH);
  buzzerActive    = true;
  buzzerStartTime = now;
}

//  Listen for CLEAR command from WinForms 
/*
  The WinForms app sends "CLEAR\r\n" over serial when the last
  active alert has been resolved. This turns off the LED.
  If a new alert arrives before the nurse resolves the current ones,
  the activeAlertCount keeps the LED on correctly.
*/
void checkSerialInput() {
  if (Serial.available() > 0) {
    String incoming = Serial.readStringUntil('\n');
    incoming.trim();

    if (incoming == "CLEAR") {
      activeAlertCount = 0;
      digitalWrite(LED_PIN,    LOW);
      digitalWrite(BUZZER_PIN, LOW);   // safety — silence buzzer too
      buzzerActive = false;
      ledOn        = false;
    }
  }
}
