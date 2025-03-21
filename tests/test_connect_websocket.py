import websockets
import asyncio
import logging

logging.basicConfig(level=logging.DEBUG)

async def main():
    uri = "ws://localhost:8080/"
    headers = {
        "Origin": "http://localhost",
    }
    try:
        async with websockets.connect(uri, compression=None) as websocket:
            print("Connected successfully.")
            
            # Send a simple message
            message = '{"id":"test1", "command":"get_unity_info", "client_timestamp":' + str(int(asyncio.get_event_loop().time() * 1000)) + '}'
            print(f"Sending: {message}")
            await websocket.send(message)
            
            # Wait for response
            response = await websocket.recv()
            print(f"Received: {response}")
            
    except Exception as ex:
        print(f"WebSocket connection failed: {ex}")

if __name__ == "__main__":
    asyncio.run(main())