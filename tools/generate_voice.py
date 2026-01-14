import os
import sys
import json
import time
import base64
# Try importing tencentcloud, if not installed, user needs to install it.
try:
    from tencentcloud.common import credential
    from tencentcloud.common.profile.client_profile import ClientProfile
    from tencentcloud.common.profile.http_profile import HttpProfile
    from tencentcloud.common.exception.tencent_cloud_sdk_exception import TencentCloudSDKException
    from tencentcloud.tts.v20190823 import tts_client, models
except ImportError:
    print("Error: tencentcloud-sdk-python is not installed.")
    print("Please run: pip install tencentcloud-sdk-python")
    sys.exit(1)

# Configuration
VOICE_LIST_FILE = os.path.join('voice', 'voice_list_add.md')
OUTPUT_DIR = os.path.join('Resources', 'voice')
VOICE_TYPE = 502003
SPEED = 2.0  # 用户指定的语速参数
VOLUME = 0.0 # Default volume
SAMPLE_RATE = 16000

def sanitize_filename(name):
    # Replace invalid characters for Windows filenames
    invalid_chars = '<>:"/\\|?*'
    for char in invalid_chars:
        name = name.replace(char, '_')
    return name.strip()

def main():
    print("--- Tencent Cloud TTS Generator ---")
    
    # 1. Get Credentials
    secret_id = os.environ.get("TENCENTCLOUD_SECRET_ID")
    secret_key = os.environ.get("TENCENTCLOUD_SECRET_KEY")

    if not secret_id or not secret_key:
        print("\nEnvironment variables TENCENTCLOUD_SECRET_ID or TENCENTCLOUD_SECRET_KEY not found.")
        print("Please enter your Tencent Cloud API credentials.")
        try:
            secret_id = input("SecretId: ").strip()
            secret_key = input("SecretKey: ").strip()
        except EOFError:
            print("\nError: Cannot read input. Please set environment variables.")
            return

    if not secret_id or not secret_key:
        print("Error: SecretId and SecretKey are required.")
        return

    # 2. Setup Client
    try:
        cred = credential.Credential(secret_id, secret_key)
        httpProfile = HttpProfile()
        httpProfile.endpoint = "tts.tencentcloudapi.com"
        clientProfile = ClientProfile()
        clientProfile.httpProfile = httpProfile
        client = tts_client.TtsClient(cred, "ap-shanghai", clientProfile)
    except Exception as e:
        print(f"Error initializing client: {e}")
        return

    # 3. Prepare Output Directory
    if not os.path.exists(OUTPUT_DIR):
        try:
            os.makedirs(OUTPUT_DIR)
            print(f"\nCreated output directory: {OUTPUT_DIR}")
        except Exception as e:
            print(f"Error creating directory {OUTPUT_DIR}: {e}")
            return

    # 4. Read Voice List
    if not os.path.exists(VOICE_LIST_FILE):
        print(f"Error: Voice list file not found at {VOICE_LIST_FILE}")
        return

    lines = []
    with open(VOICE_LIST_FILE, 'r', encoding='utf-8') as f:
        for line in f:
            text = line.strip()
            if text:
                lines.append(text)

    print(f"\nFound {len(lines)} phrases to generate.")
    print(f"Config: VoiceType={VOICE_TYPE}, Speed={SPEED}, Codec=wav")
    print("-" * 40)

    # 5. Process
    success_count = 0
    skip_count = 0
    fail_count = 0

    for i, text in enumerate(lines):
        filename = sanitize_filename(text) + ".wav"
        filepath = os.path.join(OUTPUT_DIR, filename)

        if os.path.exists(filepath):
            # Verify file size is not 0
            if os.path.getsize(filepath) > 0:
                print(f"[{i+1}/{len(lines)}] Skipped (Exists): {text}")
                skip_count += 1
                continue

        print(f"[{i+1}/{len(lines)}] Generating: {text} ...", end='', flush=True)

        try:
            req = models.TextToVoiceRequest()
            params = {
                "Text": text,
                "SessionId": f"uuid-{i}",
                "Volume": VOLUME,
                "Speed": SPEED,
                "ProjectId": 0,
                "ModelType": 1,
                "VoiceType": VOICE_TYPE,
                "PrimaryLanguage": 1,
                "SampleRate": SAMPLE_RATE,
                "Codec": "wav"
            }
            req.from_json_string(json.dumps(params))

            resp = client.TextToVoice(req)
            
            if resp.Audio:
                audio_data = base64.b64decode(resp.Audio)
                with open(filepath, 'wb') as f_audio:
                    f_audio.write(audio_data)
                print(" OK")
                success_count += 1
            else:
                print(" Failed (No Audio Data)")
                fail_count += 1
            
            # Simple rate limiting
            time.sleep(0.1)

        except TencentCloudSDKException as err:
            print(f"\nAPI Error for '{text}': {err}")
            fail_count += 1
        except Exception as err:
            print(f"\nSystem Error for '{text}': {err}")
            fail_count += 1

    print("-" * 40)
    print(f"Done. Success: {success_count}, Skipped: {skip_count}, Failed: {fail_count}")
    print(f"Files saved to: {os.path.abspath(OUTPUT_DIR)}")

if __name__ == "__main__":
    main()
