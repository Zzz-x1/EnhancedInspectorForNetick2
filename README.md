### Prerequisites
- Unity Editor 2021.3 or higher

### Steps
- Open the Unity Package Manager by navigating to Window > Package Manager along the top bar.
- Click the plus icon.
- Select Add package from git URL
- Enter https://github.com/Zzz-x1/EnhancedInspectorForNetick2.git
### Features
- View network state and modify network properties via inspector
- Compatible with custom property drawer
- ButtonAttribute for any method(support arguments)
### Preview
- Inspector
<img width="573" height="812" alt="image" src="https://github.com/user-attachments/assets/f3931831-9c30-49e2-ac18-7d9c62f7bf4d" />
<img width="576" height="519" alt="image" src="https://github.com/user-attachments/assets/4bb21d44-a206-49f7-b19d-0e4f110582e1" />

- ButtonAttribute
```C#
    [Button]
    public void Func1() { }

    [Button]
    public void Func2(int arg0, NetworkArrayStruct8<NetworkString32> arg1) { }

    [Button]
    public static void Func3() { }

    [Button]
    public static void Func3(ScriptableObject obj) { }
```

<img width="583" height="475" alt="image" src="https://github.com/user-attachments/assets/8dcff913-f4f0-4c2b-9764-65b4708d7ba8" />


