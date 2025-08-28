### ✅ Log in to Google Console

https://play.google.com/console/u/0/developers/6785796521122344722/app-list?pli=1

Deploying a .NET MAUI (Multi-platform App UI) app to the Google Play Store involves several steps. Here's a high-level guide to help you through the process:

---

### ✅ **1. Prepare Your App for Release**
Before publishing, ensure your app is production-ready:

- **Set the build configuration to Release**
- **Update the version number and version code** in `Platforms/Android/AndroidManifest.xml` or `csproj` file:
  ```xml
  <Application ... android:versionCode="1" android:versionName="1.0" />
  ```
- **Remove debugging code** and unnecessary permissions.

---

### ✅ **2. Create a Keystore and Sign the App**
Android requires apps to be signed with a keystore:

- Use the `keytool` command to generate a keystore:
  ```bash
  keytool -genkey -v -keystore myapp.keystore -alias myapp -keyalg RSA -keysize 2048 -validity 10000
  ```
- Configure signing in your `.csproj` file:
  ```xml
  <PropertyGroup>
    <AndroidKeyStore>True</AndroidKeyStore>
    <AndroidSigningKeyStore>myapp.keystore</AndroidSigningKeyStore>
    <AndroidSigningStorePass>your_store_password</AndroidSigningStorePass>
    <AndroidSigningKeyAlias>myapp</AndroidSigningKeyAlias>
    <AndroidSigningKeyPass>your_key_password</AndroidSigningKeyPass>
  </PropertyGroup>
  ```

---

### ✅ **3. Build the Android App Bundle (AAB)**
Google Play requires an `.aab` file:

```bash
dotnet publish -f:net8.0-android -c:Release -p:AndroidPackageFormat=aab
```
Make sure the code is signed by creating a cerificate.  
Dont forget to remember the password
```
jarsigner -verify -verbose -certs com.companyname.Roachagram.MobileUI-Signed.aab
```

See the project file for the build properties. You will need the passwrod in the environment variables `ROACHAGRAM_STORE_PASS` and `ROACHAGRAM_KEY_PASS`.
```

<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
  <AndroidKeyStore>True</AndroidKeyStore>
  <AndroidSigningKeyStore>upload-key.jks</AndroidSigningKeyStore>
  <AndroidSigningStorePass>$(ROACHAGRAM_STORE_PASS)</AndroidSigningStorePass>
  <AndroidSigningKeyAlias>upload-key</AndroidSigningKeyAlias>
  <AndroidSigningKeyPass>$(ROACHAGRAM_KEY_PASS)</AndroidSigningKeyPass>
</PropertyGroup>

```

The signature can be verified by this
```
jarsigner -verify -verbose -certs bin\Release\net8.0-android\publish\com.roachmachine.roachagram-Signed.aab
```

The output `.aab` file will be in: (Note the digital signature on file properties will still be empty)
```
bin\Release\net8.0-android\publish\
```

---

### ✅ **4. Create a Google Play Developer Account**
- Go to Google Play Console
- Pay the one-time registration fee
- Set up your developer profile

---

### ✅ **5. Create a New App in Google Play Console**
- Click **“Create app”**
- Fill in the app details (name, language, etc.)
- Choose app category and content rating

---

### ✅ **6. Upload Your AAB File**
- Go to **Release > Production > Create new release**
- Upload the `.aab` file
- Add release notes

---

### ✅ **7. Fill in Store Listing and Content Info**
- Add screenshots, app description, icon, feature graphic
- Complete the **Content Rating Questionnaire**
- Set up **Privacy Policy URL**

---

### ✅ **8. Submit for Review**
- Review and resolve any warnings
- Click **“Submit for review”**

---


Yes, the `keytool` **does ask you for a password** when generating a keystore. Specifically, it will prompt you for:

1. **Keystore password** – This protects the entire keystore file.
2. **Key password** – This protects the individual key (alias) within the keystore. You can choose to use the same password as the keystore or a different one.

Here’s what a typical prompt sequence looks like:

```bash
Enter keystore password:
Re-enter new password:
What is your first and last name?
[... other identity questions ...]
Enter key password for <myapp>
    (RETURN if same as keystore password):
```

Make sure to **store these passwords securely**, as you'll need them to sign your app during builds and updates. If you lose them, you won’t be able to update your app on the Play Store.

The keystore file is stored **wherever you choose to save it** when you run the `keytool` command. It does **not** have a default location unless you specify one.

For example, if you run:

```bash
keytool -genkey -v -keystore myapp.keystore -alias myapp -keyalg RSA -keysize 2048 -validity 10000
```

This will create a file named `myapp.keystore` in your **current working directory** (i.e., wherever your terminal or command prompt is pointed at when you run the command).

---

### 🔍 To find it:
- If you didn’t specify a path, check the folder where you ran the command.
- If you specified a path like `C:\Users\YourName\keystores\myapp.keystore`, it will be saved there.

---

### ✅ Best Practice:
Move the keystore to a secure and consistent location, such as:

- **Windows:** `C:\Users\<YourName>\.keystores\`
- **macOS/Linux:** `/Users/<YourName>/.keystores/` or a project-specific `keystore/` folder

Then reference that path in your `.csproj` file or build configuration.

Would you like help writing a script to generate and store the keystore in a specific location?