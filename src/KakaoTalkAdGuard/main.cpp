#include "framework.h"
#include <dwmapi.h>
#pragma comment(lib,"dwmapi.lib")
#include "KakaoTalkAdGuard.h"
#include <chrono>
#include <string>
#include <winhttp.h>
#include <tlhelp32.h>
#pragma comment(lib, "winhttp.lib")

// Global variables
HINSTANCE       hInst;
LPWSTR          szCmdLine = 0;
WCHAR           szTitle[MAX_LOADSTRING];
WCHAR           szWindowClass[MAX_LOADSTRING];
UINT            updateRate = 100;
BOOL            autoStartup = false;
BOOL            bCheckUpdate = false;
BOOL            hideTrayIcon = false;
NOTIFYICONDATA  nid = {sizeof(nid)};
BOOL            bPortable = true;
BOOL            bHideMainPannelAd = false;
BOOL            bUpdateBannerAdRegistry = false;

// Forward-declaration
ATOM             MyRegisterClass(HINSTANCE hInstance);
BOOL             InitInstance(HINSTANCE, int);
BOOL             CheckMultipleExecution(HINSTANCE hInst, HWND hWnd, WCHAR szWindowClass[MAX_LOADSTRING]);
BOOL             HideTrayIcon(HINSTANCE hInst, HWND hWnd, NOTIFYICONDATA nid);
BOOL             ToggleStartup(HWND hWnd);
BOOL             ToggleCheckUpdate(HWND hWnd);
BOOL             ToggleHideMainPannelAd(HWND hWnd);
BOOL             ToggleUpdateBannerAdRegistry(HWND hWnd);
BOOL             CheckStartup(HINSTANCE hInst, HWND hWnd);
VOID             RestoreBetaBanner();
BOOL             CreateTrayIcon(HWND hWnd, NOTIFYICONDATA* nid);
BOOL             DeleteTrayIcon(NOTIFYICONDATA nid);
VOID             ShowContextMenu(HWND hwnd, POINT pt);
LRESULT CALLBACK WndProc(HWND, UINT, WPARAM, LPARAM);
VOID CALLBACK    TimerProc(HWND hwnd, UINT message, UINT idEvent, DWORD dwTimer);
std::wstring     GetLatestReleaseTag(const std::wstring& owner, const std::wstring& repo);


int APIENTRY wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE, _In_ LPWSTR lpCmdLine, _In_ int nCmdShow) {
	// Parse command-line
	if (lpCmdLine != L"") {
		szCmdLine = lpCmdLine;
	}
	
	// Load resources
	LoadStringW(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
	if (lstrcmpW(szCmdLine, L"--restore_tray") == 0) {
		LoadStringW(hInstance, IDC_KAKAOTALKADGUARD_RESTORETRAY, szWindowClass, MAX_LOADSTRING);
	} else {
		LoadStringW(hInstance, IDC_KAKAOTALKADGUARD, szWindowClass, MAX_LOADSTRING);
	}

	// Initialize Window
	MyRegisterClass(hInstance);
	if (!InitInstance(hInstance, nCmdShow)) { return FALSE; }

	// Message loop
	MSG msg; while (GetMessage(&msg, nullptr, 0, 0)) { // Wait for new message
		TranslateMessage(&msg); // Translate WM_KEYDOWN to WM_CHAR
		DispatchMessage(&msg); // Dispatch to WndProc
	} return (int) msg.wParam;
}

ATOM MyRegisterClass(HINSTANCE hInstance) {
	WNDCLASSEXW wcex;
	wcex.cbSize = sizeof(WNDCLASSEX);
	wcex.style = CS_HREDRAW | CS_VREDRAW;
	wcex.lpfnWndProc = WndProc;
	wcex.cbClsExtra = 0;
	wcex.cbWndExtra = 0;
	wcex.hInstance = hInstance;
	wcex.hIcon = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_LOGO));
	wcex.hCursor = LoadCursor(nullptr, IDC_ARROW);
	wcex.hbrBackground = (HBRUSH) (COLOR_WINDOW + 1);
	wcex.lpszMenuName = nullptr;
	wcex.lpszClassName = szWindowClass;
	wcex.hIconSm = LoadIcon(wcex.hInstance, MAKEINTRESOURCE(IDI_LOGO));
	return RegisterClassExW(&wcex);
}

BOOL InitInstance(HINSTANCE hInstance, int nCmdShow) {
	hInst = hInstance;
	HWND hWnd = CreateWindowW(szWindowClass, szTitle, WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, nullptr, nullptr, hInstance, nullptr);
	if (!hWnd) { return FALSE; }
	/*ShowWindow(hWnd, nCmdShow);
	UpdateWindow(hWnd);*/
	return TRUE;
}

LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam) { // Called by kernel
	static HANDLE hTimer;
	static WCHAR appName[64];
	BOOL bClose = FALSE;
	BOOL bRestoretray = FALSE;

	switch (message) {
	case WM_CREATE:
		// Preprocess arguments
		if (lstrcmpW(szCmdLine, L"--startup") == 0) {
			bPortable = false;
		} else if (lstrcmpW(szCmdLine, L"--restore_tray") == 0) {
			bRestoretray = TRUE;
			HKEY key; DWORD dwDisp;
			if (RegCreateKeyEx(HKEY_CURRENT_USER, REG_CFG, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &key, &dwDisp) == ERROR_SUCCESS) {
				DWORD value = 0;
				RegSetValueExW(key, L"HideTrayIcon", 0, REG_DWORD, (const BYTE*) &value, sizeof(value));
				RegCloseKey(key);
			}
			HWND hKakaoTalkAdGuardMain = FindWindow(L"KakaoTalkAdGuard", NULL);
			if (hKakaoTalkAdGuardMain) {
				SendMessage(hKakaoTalkAdGuardMain, WM_RECHECK, NULL, NULL);
			}
			PostQuitMessage(0);
		}

		bClose = CheckMultipleExecution(hInst, hWnd, szWindowClass);
		hTimer = (HANDLE) SetTimer(hWnd, 1, updateRate, (TIMERPROC) TimerProc);
		CheckStartup(hInst, hWnd);
		if (!hideTrayIcon && !bClose && !bRestoretray) {
			CreateTrayIcon(hWnd, &nid);
		}
		break;
	case WM_NOTIFYCALLBACK:
		switch (LOWORD(lParam)) {
		case NIN_SELECT:
		case WM_CONTEXTMENU:
		{
			POINT const pt = {LOWORD(wParam), HIWORD(wParam)};
			ShowContextMenu(hWnd, pt);
		}
		break;
		}
		break;
	case WM_RECHECK:
		CheckStartup(hInst, hWnd);
		if (!hideTrayIcon) {
			CreateTrayIcon(hWnd, &nid);
		}
		break;
	case WM_INITMENU:
		CheckMenuItem((HMENU)wParam, IDM_STARTONSYSTEMSTARTUP, MF_BYCOMMAND | (autoStartup ? MF_CHECKED : MF_UNCHECKED));
		CheckMenuItem((HMENU)wParam, IDM_CHECKUPDATE, MF_BYCOMMAND | (bCheckUpdate ? MF_CHECKED : MF_UNCHECKED));
		CheckMenuItem((HMENU)wParam, IDM_HIDEMAINPANNELAD, MF_BYCOMMAND | (bHideMainPannelAd ? MF_CHECKED : MF_UNCHECKED));
		CheckMenuItem((HMENU)wParam, IDM_UPDATEBANNERADREGISTRY, MF_BYCOMMAND | (bUpdateBannerAdRegistry ? MF_CHECKED : MF_UNCHECKED));

	case WM_COMMAND:
	{
		switch (LOWORD(wParam)) {
		case IDM_HIDETRAYICON:
			HideTrayIcon(hInst, hWnd, nid);
			break;
		case IDM_STARTONSYSTEMSTARTUP:
			ToggleStartup(hWnd);
			break;
		case IDM_CHECKUPDATE:
			ToggleCheckUpdate(hWnd);
			break;
		case IDM_HIDEMAINPANNELAD:
			ToggleHideMainPannelAd(hWnd);
			break;
		case IDM_UPDATEBANNERADREGISTRY:
			ToggleUpdateBannerAdRegistry(hWnd);
			break;
		case IDM_EXIT:
			DestroyWindow(hWnd);
			break;
		default:
			return DefWindowProc(hWnd, message, wParam, lParam);
		}
	}
	break;
	case WM_DESTROY:
        RestoreBetaBanner();
		KillTimer(hWnd, 1);
		DeleteTrayIcon(nid);
		PostQuitMessage(0);
		break;
	default:
		return DefWindowProc(hWnd, message, wParam, lParam);
	}
	return 0;
}

BOOL CheckStartup(HINSTANCE hInst, HWND hWnd) {
	// Check autoruns
	HKEY key; DWORD dwDisp;
	RegCreateKeyEx(HKEY_CURRENT_USER, REG_RUN, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &key, &dwDisp);
	autoStartup = (RegQueryValueExW(key, L"KakaoTalkAdGuard", 0, NULL, 0, NULL) == NO_ERROR);
	RegCloseKey(key);
	SendMessage(hWnd, WM_INITMENU, 0, 0);
	
	// Check HideTrayIcon
	RegCreateKeyEx(HKEY_CURRENT_USER, REG_CFG, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_READ, NULL, &key, &dwDisp);
	DWORD dwType = REG_DWORD;
	DWORD dwDataSize = sizeof(DWORD);
	DWORD dwValue = 0;
	hideTrayIcon = (RegQueryValueExW(key, L"HideTrayIcon", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize) == ERROR_SUCCESS && dwValue != 0);
	bCheckUpdate = (RegQueryValueExW(key, L"CheckUpdate", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize) == ERROR_SUCCESS && dwValue != 0);
	// Default to enabled on this Qt6 beta fork; preserve user's explicit setting.
    LSTATUS adOptionStatus = RegQueryValueExW(key, L"HideMainPannelAd", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize);
    bHideMainPannelAd = (adOptionStatus == ERROR_SUCCESS) ? (dwValue != 0) : TRUE;
	bUpdateBannerAdRegistry = (RegQueryValueExW(key, L"UpdateBannerAdRegistry", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize) == ERROR_SUCCESS && dwValue != 0);

	RegCloseKey(key);

	// Check updates
	if (bCheckUpdate) {
		std::wstring latest = GetLatestReleaseTag(L"loopback-kr", L"KakaoTalkAdGuard");
		if (!latest.empty()) {
			WCHAR buffer[256] = { 0 };
			int len = LoadStringW(GetModuleHandle(NULL), IDS_APP_VERSION, buffer, _countof(buffer));
			std::wstring current = std::wstring(buffer, len);

			if (latest != current) {
				/*NOTIFYICONDATA nid = { sizeof(nid) };
				nid.uFlags = NIF_INFO;
				nid.dwInfoFlags = NIIF_INFO;
				LoadStringW(hInst, IDS_NEWUPDATE_TITLE, nid.szInfoTitle, ARRAYSIZE(nid.szInfoTitle));
				LoadStringW(hInst, IDS_NEWUPDATE_CONTENT, nid.szInfo, ARRAYSIZE(nid.szInfo));*/
				std::wstring msg = L"New update released.\n\nCurrent version: " + current + L"\nLatest version: " + latest + L"\n\n"
					L"Do you want to open the release website?";
				int ret = MessageBoxW(hWnd, msg.c_str(), L"KakaoTalk AdGuard",
					MB_YESNO | MB_ICONQUESTION);

				if (ret == IDYES) {
					ShellExecuteW(NULL, L"open", L"https://github.com/loopback-kr/KakaoTalkAdGuard/releases", NULL, NULL, SW_SHOWNORMAL);
				}
			}
		}
	}

	return 0;
}

BOOL ToggleHideMainPannelAd(HWND hWnd) {
	DWORD dwValue = 0; DWORD dwType = REG_DWORD; HKEY key; DWORD dwDisp; BOOL regHideMainPannelAd; DWORD dwDataSize = sizeof(DWORD);
	RegCreateKeyEx(HKEY_CURRENT_USER, REG_CFG, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &key, &dwDisp);
	regHideMainPannelAd = (RegQueryValueExW(key, L"HideMainPannelAd", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize) == ERROR_SUCCESS && dwValue != 0);
	bHideMainPannelAd = not regHideMainPannelAd;
    if(!bHideMainPannelAd)RestoreBetaBanner();
	RegSetValueExW(key, L"HideMainPannelAd", 0, REG_DWORD, (const BYTE*)&bHideMainPannelAd, sizeof(bHideMainPannelAd));
	RegCloseKey(key);
	return 0;
}
BOOL ToggleUpdateBannerAdRegistry(HWND hWnd) {
	DWORD dwValue = 0; DWORD dwType = REG_DWORD; HKEY key; DWORD dwDisp; BOOL regUpdateBannerAdRegistry; DWORD dwDataSize = sizeof(DWORD);
	RegCreateKeyEx(HKEY_CURRENT_USER, REG_CFG, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &key, &dwDisp);
	regUpdateBannerAdRegistry = (RegQueryValueExW(key, L"UpdateBannerAdRegistry", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize) == ERROR_SUCCESS && dwValue != 0);
	bUpdateBannerAdRegistry = not regUpdateBannerAdRegistry;
	RegSetValueExW(key, L"UpdateBannerAdRegistry", 0, REG_DWORD, (const BYTE*)&bUpdateBannerAdRegistry, sizeof(bUpdateBannerAdRegistry));
	RegCloseKey(key);
	return 0;
}

BOOL ToggleCheckUpdate(HWND hWnd) {
	DWORD dwValue = 0; DWORD dwType = REG_DWORD; HKEY key; DWORD dwDisp; BOOL regCheckUpdate; DWORD dwDataSize = sizeof(DWORD);
	RegCreateKeyEx(HKEY_CURRENT_USER, REG_CFG, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &key, &dwDisp);
	regCheckUpdate = (RegQueryValueExW(key, L"CheckUpdate", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize) == ERROR_SUCCESS && dwValue != 0);
	bCheckUpdate = not regCheckUpdate;
	RegSetValueExW(key, L"CheckUpdate", 0, REG_DWORD, (const BYTE*)&bCheckUpdate, sizeof(bCheckUpdate));
	RegCloseKey(key);
	return 0;
}

BOOL ToggleStartup(HWND hWnd) {
	HKEY key; DWORD dwDisp;
	RegCreateKeyEx(HKEY_CURRENT_USER, REG_RUN, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &key, &dwDisp);

	if (RegQueryValueExW(key, L"KakaoTalkAdGuard", 0, NULL, 0, NULL) == NO_ERROR) {
		RegDeleteValueW(key, L"KakaoTalkAdGuard");
		autoStartup = false;
	} else {
		WCHAR szFileName[MAX_PATH];
		WCHAR szFileNameFinal[MAX_PATH] = L"\"";
		GetModuleFileName(NULL, szFileName, MAX_PATH);
		lstrcatW(szFileNameFinal, szFileName);
		lstrcatW(szFileNameFinal, L"\"");
		lstrcatW(szFileNameFinal, L" --startup");
		RegSetValueExW(key, L"KakaoTalkAdGuard", 0, REG_SZ, (LPBYTE) szFileNameFinal, (lstrlenW(szFileNameFinal) + 1) * sizeof(WCHAR));
		autoStartup = true;
	}
	RegCloseKey(key);
	return 0;
}

BOOL HideTrayIcon(HINSTANCE hInst, HWND hWnd, NOTIFYICONDATA nid) {
	WCHAR msgboxHideTray[MAX_LOADSTRING];
	LoadStringW(hInst, IDS_MSGBOX_HIDETRAY, msgboxHideTray, MAX_LOADSTRING);
	MessageBox(hWnd, msgboxHideTray, NULL, MB_ICONWARNING);
	HKEY key; DWORD dwDisp;
	LSTATUS ret;
	if (RegCreateKeyEx(HKEY_CURRENT_USER, REG_CFG, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &key, &dwDisp) == ERROR_SUCCESS) {
		DWORD value = 1;
		ret = RegSetValueExW(key, L"HideTrayIcon", 0, REG_DWORD, (const BYTE*) &value, sizeof(value));
		RegCloseKey(key);
	}
	DeleteTrayIcon(nid);
	return 0;
}

BOOL CheckMultipleExecution(HINSTANCE hInst, HWND hWnd, WCHAR szWindowClass[MAX_LOADSTRING]) {
	CreateMutex(NULL, TRUE, szWindowClass);
	if (GetLastError() == ERROR_ALREADY_EXISTS) {
		WCHAR msgboxIsRunning[MAX_LOADSTRING];
		LoadStringW(hInst, IDS_MSGBOX_ISRUNNING, msgboxIsRunning, MAX_LOADSTRING);
		MessageBox(hWnd, msgboxIsRunning, NULL, MB_ICONWARNING);
		PostQuitMessage(0);
		return 1;
	}
	return 0;
}

BOOL CreateTrayIcon(HWND hWnd, NOTIFYICONDATA* nid) {
	nid->hWnd = hWnd;
	nid->uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE | NIF_SHOWTIP;
	LoadString(hInst, IDS_APP_NAME, nid->szTip, ARRAYSIZE(nid->szTip));
	nid->hIcon = LoadIcon(hInst, MAKEINTRESOURCE(IDI_LOGO));
	nid->uCallbackMessage = WM_NOTIFYCALLBACK;
	Shell_NotifyIcon(NIM_ADD, nid);
	nid->uVersion = NOTIFYICON_VERSION_4;
	return Shell_NotifyIcon(NIM_SETVERSION, nid);
}

BOOL DeleteTrayIcon(NOTIFYICONDATA nid) {
	return Shell_NotifyIcon(NIM_DELETE, &nid);
}

void ShowContextMenu(HWND hwnd, POINT pt) {
	HMENU hMenu;
	WCHAR szAppName[MAX_LOADSTRING];
	WCHAR szVersion[MAX_LOADSTRING];
	WCHAR szFullAppName[MAX_LOADSTRING];
	szFullAppName[0] = L'\0';
	LoadStringW(hInst, IDS_APP_NAME, szAppName, MAX_LOADSTRING);
	LoadStringW(hInst, IDS_APP_VERSION, szVersion, MAX_LOADSTRING);
	wcscpy_s(szFullAppName, szAppName);
	wcscat_s(szFullAppName, L" ");
	wcscat_s(szFullAppName, szVersion);

	if (bPortable) {
		hMenu = LoadMenu(hInst, MAKEINTRESOURCE(IDR_TRAY_CONTEXTMENU_PORTABLE));
		ModifyMenuW(hMenu, ID__APP_TITLE, MF_BYCOMMAND | MF_STRING | MF_DISABLED, ID__APP_TITLE, (LPCWSTR) szFullAppName);
	} else {
		hMenu = LoadMenu(hInst, MAKEINTRESOURCE(IDR_TRAY_CONTEXTMENU));
		ModifyMenuW(hMenu, ID__APP_TITLE, MF_BYCOMMAND | MF_STRING | MF_DISABLED, ID__APP_TITLE, (LPCWSTR)szFullAppName);
	}
	HMENU hSubMenu = GetSubMenu(hMenu, 0);
	SetForegroundWindow(hwnd); // our window must be foreground before calling TrackPopupMenu or the menu will not disappear when the user clicks away
	// respect menu drop alignment
	UINT uFlags = TPM_RIGHTBUTTON;
	if (GetSystemMetrics(SM_MENUDROPALIGNMENT) != 0) {
		uFlags |= TPM_RIGHTALIGN;
	} else {
		uFlags |= TPM_LEFTALIGN;
	}
	TrackPopupMenuEx(hSubMenu, uFlags, pt.x, pt.y, hwnd, NULL);
	DestroyMenu(hMenu);
}

HWND hKakaoTalkMain;
HWND hAdFit;
RECT RectKakaoTalkMain;
BOOL g_bFoundWebAd = FALSE;

typedef struct {
	LPCWSTR targetClassName;
	BOOL bFoundTarget;
} FIND_DATA;


BOOL CALLBACK FindChromeAndHide(HWND hwndChild, LPARAM lParam) {
	FIND_DATA* pFindClassName = reinterpret_cast<FIND_DATA*>(lParam);
	WCHAR className[256];
	if (GetClassName(hwndChild, className, 256) > 0) {
		if (wcsncmp(className, pFindClassName->targetClassName, 17) == 0) {
			ShowWindow(hwndChild, SW_HIDE);
			g_bFoundWebAd = TRUE;
			return FALSE;
		}
	}
	EnumChildWindows(hwndChild, FindChromeAndHide, lParam);

	return TRUE;
}

BOOL CALLBACK EnumWindowProc(HWND hwnd, LPARAM lParam) {
	FIND_DATA* pFindClassName = reinterpret_cast<FIND_DATA*>(lParam);
	HWND parentHandle = GetParent(hwnd);
	WCHAR className[256];
	WCHAR windowText[256] = L"";
	GetClassName(hwnd, className, 256);
	GetWindowText(hwnd, windowText, 256);

	if (wcscmp(className, pFindClassName->targetClassName) == 0 && wcscmp(windowText, L"") == 0) {
		FIND_DATA targetClassName = { L"Chrome_WidgetWin_", FALSE };
		g_bFoundWebAd = FALSE;
		EnumChildWindows(hwnd, FindChromeAndHide, (LPARAM)&targetClassName);
		if (g_bFoundWebAd) {
			ShowWindow(hwnd, SW_HIDE);
		}
	}
	return TRUE;
}

BOOL CALLBACK EnumChildProc(HWND hwnd, LPARAM lParam) {
	HWND parentHandle = GetParent(hwnd);
	WCHAR className[256] = L"";
	WCHAR parentClassName[256] = L"";
	WCHAR windowText[256] = L"";
	GetClassName(hwnd, className, 256);
	GetWindowText(hwnd, windowText, 256);
	RECT Recthwnd;

	if (wcscmp(className, L"EVA_ChildWindow") == 0) {
		if (wcsncmp(windowText, L"OnlineMainView_", 15) == 0) { // Expand chat widget to empty space
			SetWindowPos(hwnd, HWND_TOP, 0, 0, (RectKakaoTalkMain.right - RectKakaoTalkMain.left), (RectKakaoTalkMain.bottom - RectKakaoTalkMain.top - 32), SWP_NOMOVE);
		}
		return TRUE;
	}
	if (wcsncmp(windowText, L"LockModeView_", 13) == 0) { // Expand numpad in Lockdown mode
		HWND hLockdownNumpad = FindWindowEx(hwnd, NULL, L"EVA_ChildWindow", L"");
		if (hLockdownNumpad != NULL)
			SetWindowPos(hwnd, HWND_TOP, 0, 0, (RectKakaoTalkMain.right - RectKakaoTalkMain.left), (RectKakaoTalkMain.bottom - RectKakaoTalkMain.top), SWP_NOMOVE);
	}
	InvalidateRect(parentHandle, NULL, TRUE);

	return TRUE;
}

void SetLUDForAllSubkeys() {
	HKEY hParentKey;
	if (RegOpenKeyExW(HKEY_CURRENT_USER, L"SOFTWARE\\Kakao\\AdFit", 0, KEY_READ | KEY_WRITE | KEY_ENUMERATE_SUB_KEYS, &hParentKey) != ERROR_SUCCESS) {
		return;
	}

	DWORD dwIndex = 0;
	wchar_t subkeyName[256];
	DWORD subkeyNameSize = sizeof(subkeyName) / sizeof(wchar_t);
	FILETIME ftLastWritten;

	while (RegEnumKeyExW(hParentKey, dwIndex, subkeyName, &subkeyNameSize, NULL, NULL, NULL, &ftLastWritten) == ERROR_SUCCESS) {

		std::wstring subkeyPath = L"SOFTWARE\\Kakao\\AdFit\\";
		subkeyPath += subkeyName;

		HKEY hSubKey;
		DWORD dwDisp;
		if (RegCreateKeyExW(HKEY_CURRENT_USER, subkeyPath.c_str(), 0, NULL, REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &hSubKey, &dwDisp) == ERROR_SUCCESS) {

			auto now = std::chrono::system_clock::now();
			auto posix_time = std::chrono::duration_cast<std::chrono::seconds>(now.time_since_epoch()).count();
			std::wstring str_time = std::to_wstring(posix_time);

			RegSetValueExW(hSubKey, L"LUD", 0, REG_SZ, (const BYTE*)str_time.c_str(), (str_time.length() + 1) * sizeof(wchar_t));
			RegCloseKey(hSubKey);
		}

		dwIndex++;
		subkeyNameSize = sizeof(subkeyName) / sizeof(wchar_t);
	}
	RegCloseKey(hParentKey);
}

std::wstring GetLatestReleaseTag(const std::wstring& owner, const std::wstring& repo) {
	HINTERNET hSession = WinHttpOpen(L"KakaoTalkAdGuard/1.0",
		WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
		WINHTTP_NO_PROXY_NAME,
		WINHTTP_NO_PROXY_BYPASS,
		0);
	if (!hSession) return L"";

	HINTERNET hConnect = WinHttpConnect(hSession, L"api.github.com",
		INTERNET_DEFAULT_HTTPS_PORT, 0);
	if (!hConnect) {
		WinHttpCloseHandle(hSession);
		return L"";
	}

	std::wstring path = L"/repos/" + owner + L"/" + repo + L"/releases/latest";
	HINTERNET hRequest = WinHttpOpenRequest(hConnect, L"GET", path.c_str(),
		NULL, WINHTTP_NO_REFERER,
		WINHTTP_DEFAULT_ACCEPT_TYPES,
		WINHTTP_FLAG_SECURE);
	if (!hRequest) {
		WinHttpCloseHandle(hConnect);
		WinHttpCloseHandle(hSession);
		return L"";
	}

	const wchar_t* userAgentHeader = L"User-Agent: Win32App";
	WinHttpAddRequestHeaders(hRequest, userAgentHeader, -1, WINHTTP_ADDREQ_FLAG_ADD);

	BOOL bResults = WinHttpSendRequest(hRequest,
		WINHTTP_NO_ADDITIONAL_HEADERS, 0,
		WINHTTP_NO_REQUEST_DATA, 0,
		0, 0);

	if (bResults) bResults = WinHttpReceiveResponse(hRequest, NULL);

	std::wstring response;
	if (bResults) {
		DWORD dwSize = 0;
		do {
			DWORD dwDownloaded = 0;
			if (!WinHttpQueryDataAvailable(hRequest, &dwSize)) break;

			if (dwSize == 0) break;

			char* buffer = new char[dwSize + 1];
			ZeroMemory(buffer, dwSize + 1);

			if (WinHttpReadData(hRequest, buffer, dwSize, &dwDownloaded)) {
				std::wstring wbuf;
				int len = MultiByteToWideChar(CP_UTF8, 0, buffer, dwDownloaded, NULL, 0);
				if (len > 0) {
					wbuf.resize(len);
					MultiByteToWideChar(CP_UTF8, 0, buffer, dwDownloaded, &wbuf[0], len);
					response += wbuf;
				}
			}
			delete[] buffer;
		} while (dwSize > 0);
	}

	WinHttpCloseHandle(hRequest);
	WinHttpCloseHandle(hConnect);
	WinHttpCloseHandle(hSession);

	std::wstring tag = L"";
	size_t pos = response.find(L"\"tag_name\"");
	if (pos != std::wstring::npos) {
		size_t start = response.find(L"\"", pos + 10);
		size_t end = response.find(L"\"", start + 1);
		if (start != std::wstring::npos && end != std::wstring::npos) {
			tag = response.substr(start + 1, end - start - 1);
		}
	}

	return tag;
}


// Qt6 beta compatibility. All Qt windows are checked against the actual
// KakaoTalkUI.exe process and a separate Chrome-bearing banner.
// Deliberately no outer-window resizing/SetWindowRgn/overlay: QML list items
// aren't native EVA_ChildWindow windows and cannot safely be resized by Win32.
struct BETA_MAIN {
    DWORD pid=0;
    HWND hwnd=NULL;
    RECT rect={0};
};
static bool IsBetaProcess(DWORD pid) {
    if(!pid)return false;
    HANDLE proc=OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION,FALSE,pid);
    if(!proc)return false;
    WCHAR path[MAX_PATH]={0};DWORD size=MAX_PATH;
    BOOL ok=QueryFullProcessImageNameW(proc,0,path,&size);
    CloseHandle(proc);
    if(!ok)return false;
    const WCHAR* filename=wcsrchr(path,L'\\');
    filename=filename?filename+1:path;
    return _wcsicmp(filename,L"KakaoTalkUI.exe")==0;
}
BOOL CALLBACK FindQtBetaMain(HWND h, LPARAM param) {
    auto* ctx=reinterpret_cast<BETA_MAIN*>(param);
    WCHAR klass[120]={0};WCHAR title[120]={0};
    if(!IsWindowVisible(h))return TRUE;
    if(!GetClassNameW(h,klass,120) || wcsstr(klass,L"QWindowIcon")==nullptr)return TRUE;
    RECT r;if(!GetWindowRect(h,&r))return TRUE;
    int width=r.right-r.left,height=r.bottom-r.top;
    if(width<350 || width>700 || height<470 || height>1800)return TRUE;
    GetWindowTextW(h,title,120);
    if(wcscmp(title,L"카카오톡")!=0 && wcscmp(title,L"KakaoTalk")!=0 &&
       wcscmp(title,L"カカオトーク")!=0) return TRUE;
    DWORD pid=0;GetWindowThreadProcessId(h,&pid);
    if(!IsBetaProcess(pid))return TRUE;
    ctx->pid=pid;ctx->hwnd=h;ctx->rect=r;
    return FALSE;
}
static bool g_restoreBetaBanner=false;
static bool gBetaAdMeasured=false;
static int gBetaAdHeight=0;
struct BETA_CHILD_STATE{bool chrome=false;};
BOOL CALLBACK FindBetaChrome(HWND child,LPARAM param) {
    auto* result=reinterpret_cast<BETA_CHILD_STATE*>(param);
    WCHAR klass[120]={0};GetClassNameW(child,klass,120);
    if(wcsncmp(klass,L"Chrome_WidgetWin_",17)==0) {
        result->chrome=true;return FALSE;
    }
    return TRUE;
}
BOOL CALLBACK HideQtBetaAd(HWND h,LPARAM param) {
    auto* main=reinterpret_cast<BETA_MAIN*>(param);
    if(h==main->hwnd)return TRUE;
    DWORD pid=0;GetWindowThreadProcessId(h,&pid);
    if(pid!=main->pid)return TRUE;
    WCHAR klass[120]={0};GetClassNameW(h,klass,120);
    if(wcsstr(klass,L"QWindowIcon")==nullptr)return TRUE;
    RECT r;if(!GetWindowRect(h,&r))return TRUE;
    int mw=main->rect.right-main->rect.left;
    int width=r.right-r.left,height=r.bottom-r.top;
    int gap=8;
    if(abs(r.left-(main->rect.left+gap))>8 ||
       abs(width-(mw-2*gap))>12 ||
       abs(height-100)>14 ||
       abs(main->rect.bottom-(r.bottom+gap))>25)return TRUE;
    BETA_CHILD_STATE child;
    EnumChildWindows(h,FindBetaChrome,reinterpret_cast<LPARAM>(&child));
    if(!child.chrome)return TRUE;
    if(g_restoreBetaBanner) {
        if(!IsWindowVisible(h))ShowWindow(h,SW_SHOWNOACTIVATE);
    } else {
        if(IsWindowVisible(h))ShowWindow(h,SW_HIDE);
        gBetaAdMeasured=true;gBetaAdHeight=height;
    }
    return TRUE;
}
VOID RestoreBetaVisual();

// Qt6 native layout reflow: the existing 100px advertisement loader and 1px
// separator are hidden inside the KakaoTalk GUI thread. Qt ColumnLayout then
// gives the complete original 677px main content height to StackView.
// This does NOT enlarge, clip, or overlay the KakaoTalk window.
static HMODULE gQtLayoutModule=nullptr;
static HHOOK gQtLayoutHook=nullptr;
static HWND gQtLayoutMain=nullptr;
static DWORD gQtLayoutSentTick=0;
static void StopQt6LayoutHook() {
 if(gQtLayoutHook) {
    if(gQtLayoutMain && IsWindow(gQtLayoutMain)) {
        PostMessageW(gQtLayoutMain,WM_APP+101,0,0);
        Sleep(240); // Give KakaoTalk UI thread a chance to restore the layout.
    }
    UnhookWindowsHookEx(gQtLayoutHook);
    gQtLayoutHook=nullptr;
 }
 if(gQtLayoutModule){FreeLibrary(gQtLayoutModule);gQtLayoutModule=nullptr;}
 gQtLayoutMain=nullptr;gQtLayoutSentTick=0;
}
static void EnsureQt6LayoutHook(HWND mainWindow) {
 if(!mainWindow || !IsWindow(mainWindow))return;
 if(gQtLayoutHook && gQtLayoutMain==mainWindow) {
    if(GetTickCount()-gQtLayoutSentTick>6000) {
       PostMessageW(mainWindow,WM_APP+100,0,0);
       gQtLayoutSentTick=GetTickCount();
    }
    return;
 }
 if(gQtLayoutHook)StopQt6LayoutHook();
 wchar_t self[MAX_PATH]={};
 if(!GetModuleFileNameW(nullptr,self,MAX_PATH))return;
 std::wstring libPath(self);
 auto slash=libPath.find_last_of(L"\\/");
 if(slash==std::wstring::npos)return;
 libPath.resize(slash+1);
 libPath+=L"SoNGQt6Layout.dll";
 HMODULE dll=LoadLibraryW(libPath.c_str());
 if(!dll)return;
 auto callback=(HOOKPROC)GetProcAddress(dll,"QtProbeHook");
 DWORD process=0;
 DWORD tid=GetWindowThreadProcessId(mainWindow,&process);
 if(!callback || !tid){FreeLibrary(dll);return;}
 HHOOK hook=SetWindowsHookExW(WH_GETMESSAGE,callback,dll,tid);
 if(!hook){FreeLibrary(dll);return;}
 gQtLayoutModule=dll;gQtLayoutHook=hook;gQtLayoutMain=mainWindow;
 gQtLayoutSentTick=GetTickCount();
 PostMessageW(mainWindow,WM_APP+100,0,0);
}

VOID RestoreBetaBanner() {
    StopQt6LayoutHook();
    RestoreBetaVisual();
    BETA_MAIN beta;
    EnumWindows(FindQtBetaMain,reinterpret_cast<LPARAM>(&beta));
    if(beta.hwnd) {
        g_restoreBetaBanner=true;
        EnumWindows(HideQtBetaAd,reinterpret_cast<LPARAM>(&beta));
        g_restoreBetaBanner=false;
    }
}


// Beta-only cosmetic clipping. This does not resize Qt QML items.
// Trial first; restore the entire region/style on exit or disable.
struct BetaVisualClip {
 HWND main=nullptr;
 LONG_PTR initialStyle=0;
 bool styleSaved=false;
 int mainW=0, mainH=0, crop=0;
 bool clipped=false;
};
static BetaVisualClip gBetaVisual;
void RestoreBetaVisual() {
 if(gBetaVisual.main && IsWindow(gBetaVisual.main)) {
    HWND h=gBetaVisual.main;
    SetWindowRgn(h,nullptr,TRUE);
    if(gBetaVisual.styleSaved) {
        LONG_PTR state=GetWindowLongPtrW(h,GWL_STYLE) & (WS_MAXIMIZE|WS_MINIMIZE);
        SetWindowLongPtrW(h,GWL_STYLE,(gBetaVisual.initialStyle & ~(WS_MAXIMIZE|WS_MINIMIZE))|state);
        SetWindowPos(h,nullptr,0,0,0,0,
            SWP_NOSIZE|SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_FRAMECHANGED);
    }
    int borderPref=0;
    DwmSetWindowAttribute(h,33,&borderPref,sizeof(borderPref));
 }
 gBetaVisual=BetaVisualClip{};
 gBetaAdMeasured=false;gBetaAdHeight=0;
}
void ClipQtBetaWhitespace(HWND h) {
 if(!h || !IsWindow(h))return;
 if(IsZoomed(h)){
    if(gBetaVisual.clipped)RestoreBetaVisual();
    return;
 }
 if(!gBetaAdMeasured)return;
 RECT r;if(!GetWindowRect(h,&r))return;
 int w=r.right-r.left, ht=r.bottom-r.top;
 if(w<300||ht<400)return;
 int crop=gBetaAdHeight+14;
 if(crop<55||crop>160||ht-crop<320)return;
 if(gBetaVisual.main && gBetaVisual.main!=h)RestoreBetaVisual();
 if(gBetaVisual.clipped && gBetaVisual.main==h &&
    gBetaVisual.mainW==w && gBetaVisual.mainH==ht &&
    gBetaVisual.crop==crop)return;
 if(!gBetaVisual.styleSaved){
    gBetaVisual.initialStyle=GetWindowLongPtrW(h,GWL_STYLE);
    gBetaVisual.styleSaved=true;
 }
 // On builds where DWM cannot move the bottom border to the clipped edge,
 // remove the native frame. This avoids the phantom transparent boundary.
 LONG_PTR oldStyle=GetWindowLongPtrW(h,GWL_STYLE);
 LONG_PTR noFrame=oldStyle & ~(WS_CAPTION|WS_THICKFRAME);
 if(oldStyle!=noFrame) {
    SetWindowLongPtrW(h,GWL_STYLE,noFrame);
    SetWindowPos(h,nullptr,0,0,0,0,
        SWP_NOSIZE|SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_FRAMECHANGED);
 }
 int squareCorner=1;
 DwmSetWindowAttribute(h,33,&squareCorner,sizeof(squareCorner));
 HRGN rg=CreateRectRgn(0,0,w,ht-crop);
 if(!rg)return;
 if(!SetWindowRgn(h,rg,TRUE)){DeleteObject(rg);return;}
 gBetaVisual.main=h;gBetaVisual.mainW=w;gBetaVisual.mainH=ht;
 gBetaVisual.crop=crop;gBetaVisual.clipped=true;
}

// Magnetic window edges without resizing the Qt main HWND. Unlike the
// experimental .NET helper, this leaves its real size equal to its DWM size.
static HWND betaLastMain=nullptr;
static RECT betaLastPosition={0};
static DWORD betaLastMovement=0;
static bool betaSnapPending=true;
void KeepQtBetaInsideWorkArea(HWND hwnd) {
    if(!hwnd || IsIconic(hwnd) || IsZoomed(hwnd))return;
    RECT r;if(!GetWindowRect(hwnd,&r))return;
    if(hwnd!=betaLastMain) {
        betaLastMain=hwnd;betaLastPosition=r;
        betaLastMovement=GetTickCount();betaSnapPending=true;return;
    }
    if(r.left!=betaLastPosition.left || r.top!=betaLastPosition.top ||
       r.right!=betaLastPosition.right || r.bottom!=betaLastPosition.bottom) {
        betaLastPosition=r;betaLastMovement=GetTickCount();betaSnapPending=true;return;
    }
    if(!betaSnapPending || (GetAsyncKeyState(VK_LBUTTON)&0x8000))return;
    if(GetTickCount()-betaLastMovement<150)return;
    betaSnapPending=false;
    HMONITOR mon=MonitorFromWindow(hwnd,MONITOR_DEFAULTTONEAREST);
    MONITORINFO info={sizeof(info)};
    if(!GetMonitorInfoW(mon,&info))return;
    RECT area=info.rcWork;
    int w=r.right-r.left,h=r.bottom-r.top;
    if(gBetaVisual.main==hwnd && gBetaVisual.clipped)
        h-=gBetaVisual.crop;
    const int marginX=8, marginTop=1, marginBottom=8;
    int leftMin=area.left-marginX;
    int leftMax=area.right-(w-marginX);
    int topMin=area.top-marginTop;
    int topMax=area.bottom-(h-marginBottom);
    if(leftMax<leftMin)leftMax=leftMin;
    if(topMax<topMin)topMax=topMin;
    int x=max(leftMin,min(leftMax,r.left));
    int y=max(topMin,min(topMax,r.top));
    const int reach=22;
    if(abs(x-leftMin)<=reach)x=leftMin;
    else if(abs(x-leftMax)<=reach)x=leftMax;
    if(abs(y-topMin)<=reach)y=topMin;
    else if(abs(y-topMax)<=reach)y=topMax;
    if(x!=r.left || y!=r.top) {
        SetWindowPos(hwnd,NULL,x,y,0,0,
            SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_NOOWNERZORDER);
        betaLastPosition.left=x;betaLastPosition.top=y;
        betaLastPosition.right=x+w;betaLastPosition.bottom=y+h;
    }
}

VOID CALLBACK TimerProc(HWND hwnd, UINT message, UINT idEvent, DWORD dwTimer) {
	switch (idEvent) {
	case 1: // Remove KakaoTalk ADs
		// Find main handle
		const WCHAR* kakaoTalkNames[] = {L"카카오톡", L"カカオトーク", L"KakaoTalk"};
		int numNames = sizeof(kakaoTalkNames) / sizeof(kakaoTalkNames[0]);
		for (int i = 0; i < numNames; ++i) {
			hKakaoTalkMain = FindWindow(L"EVA_Window_Dblclk", kakaoTalkNames[i]);
			if (hKakaoTalkMain != NULL)
				break;
		}

		// Block popup AD
		if (bUpdateBannerAdRegistry == 1) {
			SetLUDForAllSubkeys();
		}

        // Original EVA behavior is retained for stable KakaoTalk 4.x.
        if (hKakaoTalkMain != NULL && bHideMainPannelAd) {
            GetWindowRect(hKakaoTalkMain, &RectKakaoTalkMain);
            FIND_DATA targetClassName = {L"EVA_Window_Dblclk", FALSE};
            EnumWindows(EnumWindowProc,(LPARAM)&targetClassName);
            EnumChildWindows(hKakaoTalkMain,EnumChildProc,NULL);
        }
        // New Qt6 beta path: retain ONLY safe original-style ad window hiding.
        // Qt6 QQuickItems (ChatRoomList/ContactList) aren't HWND child windows.
        // Never resize or clip the KakaoTalk main HWND to fake a longer list.
        if(bHideMainPannelAd) {
            BETA_MAIN beta;
            EnumWindows(FindQtBetaMain,reinterpret_cast<LPARAM>(&beta));
            if(beta.hwnd) {
                EnumWindows(HideQtBetaAd,reinterpret_cast<LPARAM>(&beta));
                EnsureQt6LayoutHook(beta.hwnd);
            }
        } else {
            StopQt6LayoutHook();
            RestoreBetaVisual();
        }
        // Independent from the advertisement setting: keep Qt main fully
        // inside its monitor's work area and magnetically align near edges.
        BETA_MAIN currentBeta;
        EnumWindows(FindQtBetaMain,reinterpret_cast<LPARAM>(&currentBeta));
        if(currentBeta.hwnd)KeepQtBetaInsideWorkArea(currentBeta.hwnd);
		break;
	}
}
