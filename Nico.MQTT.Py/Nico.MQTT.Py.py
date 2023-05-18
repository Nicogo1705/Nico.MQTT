import paho.mqtt.client as mqtt

def callbackTopicTempChambre(client, userdata, msg):
    print(f"La temperature de la chambre est de {msg.payload}")

def callbackTopicTempBureau(client, userdata, msg):
    print(f"La temperature du bureau est de {msg.payload}")

def callbackParDefaut(client, userdata, msg):
    print(f"Message recu sur le topic {msg.topic}: {msg.payload}")

raspberrypithib = mqtt.Client()
raspberrypithib.connect("192.168.2.133", 1883)
raspberrypithib.subscribe("temp/#")

raspberrypithib.message_callback_add("temp/chambre", callbackTopicTempChambre)
raspberrypithib.message_callback_add("temp/bureau", callbackTopicTempBureau)
raspberrypithib.on_message = callbackParDefaut
raspberrypithib.loop_forever()