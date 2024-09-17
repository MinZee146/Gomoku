import socket
import threading
import random
import time

SERVER_IP = "0.0.0.0"
PORT = 5050
ADDR = (SERVER_IP, PORT)
HEADER = 1024
waiting_clients = []

class GameSession:
    def __init__(self, player1, player2):
        self.players = [player1, player2]
        self.readyPlayer = [False, False]
        self.threads = []
        time.sleep(0.5)
        self.assign_roles()
        self.run()

    def assign_roles(self):
        numbers = [1, 2]
        random.shuffle(numbers)

        for i, player in enumerate(self.players):
            player.send(f"[INITIALIZE] {numbers[i]}".encode("utf-8"))

    def run(self):
        for player in self.players:
            thread = threading.Thread(target=self.handle_player, args=(player,))
            thread.start()
            self.threads.append(thread)

        for thread in self.threads:
            thread.join()

    def handle_player(self, player):
        try:
            while True:
                otherPlayer = self.players[1 - self.players.index(player)]

                if player.fileno() == -1:
                    return

                try:
                    request = player.recv(HEADER).decode("utf-8")
                    print(request)
                except:
                    if otherPlayer.fileno() != -1:
                        otherPlayer.send("[DISCONNECTED]".encode("utf-8"))

                    player.close()
                    otherPlayer.close()
                    return

                if request.startswith("[SPAWN]"):
                    otherPlayer.send(request.encode("utf-8"))

                elif request.startswith("[GAMEOVER]"):
                    time.sleep(0.5)
                    otherPlayer.send(request.encode("utf-8"))

                elif request.startswith("[RESTART_REQUEST]"):
                    time.sleep(0.5)
                    otherPlayer.send(request.encode("utf-8"))

                elif request.startswith("[RESTART_YES]"):
                    otherPlayer.send(request.encode("utf-8"))
                    time.sleep(1)
                    self.assign_roles()

                elif request.startswith("[RESTART_NO]"):
                    otherPlayer.send(request.encode("utf-8"))
                    player.close()
                    otherPlayer.close()
                    return
                
                elif request.startswith("[RESTART_CANCEL]"):
                    otherPlayer.send(request.encode("utf-8"))
                    player.close()
                    otherPlayer.close()
                    return

                elif request.startswith("[DISCONNECTED]"):
                    otherPlayer.send("[DISCONNECTED]".encode("utf-8"))
                    player.close()
                    otherPlayer.close()
                    return
        finally:
            player.close()

def run_server():
    try:
        server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server.bind((SERVER_IP, PORT))

        server.listen()
        print(f"Listening on {SERVER_IP}:{PORT}")

        while True:
            client_socket, addr = server.accept()
            print(f"Accepted connection from {addr[0]}:{addr[1]}")
            client_socket.send("[WAITING]".encode("utf-8"))
            waiting_clients.append(client_socket)
            print(f"Number of waiting clients: {len(waiting_clients)}")

            if len(waiting_clients) >= 2:
                player1 = waiting_clients.pop(0)
                player2 = waiting_clients.pop(0)
                game = GameSession(player1, player2)

    except Exception as e:
        print(f"Error: {e}")
    finally:
        server.close()

run_server()