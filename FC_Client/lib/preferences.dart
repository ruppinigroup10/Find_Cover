import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:geolocator/geolocator.dart';
import 'base_page.dart';
import 'local_storage_service.dart';
import 'settings.dart'; // ייבוא עמוד ההגדרות

class PreferencesPage extends StatefulWidget {
  const PreferencesPage({super.key});

  @override
  State<PreferencesPage> createState() => _PreferencesPageState();
}

class _PreferencesPageState extends State<PreferencesPage> {
  // State variables for preferences
  String? preferredShelterType;
  String? accessibilityRequired;
  String? petsAllowed;
  bool isLoading = true;

  // Options
  final List<String> shelterTypes = ['הכל', 'פרטי', 'ציבורי'];
  final List<String> yesNoOptions = ['לא', 'כן'];

  // הוסף למעלה ב-class:
  final GlobalKey _dropdownFieldKey = GlobalKey();
  Timer? _locationCheckTimer;

  @override
  void initState() {
    super.initState();
    _fetchPreferences();
    _startLocationServiceCheck();
  }

  void _startLocationServiceCheck() {
    _locationCheckTimer?.cancel();
    _locationCheckTimer = Timer.periodic(const Duration(minutes: 5), (
      timer,
    ) async {
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled && mounted) {
        if (context.mounted) {
          showDialog(
            context: context,
            barrierDismissible: false,
            builder:
                (ctx) => AlertDialog(
                  title: const Text('שירותי מיקום כבויים'),
                  content: const Text(
                    'כדי להמשיך להשתמש באפליקציה, יש להפעיל את שירותי המיקום במכשיר שלך.',
                  ),
                  actions: [
                    TextButton(
                      onPressed: () async {
                        await Geolocator.openLocationSettings();
                        Navigator.of(ctx).pop();
                      },
                      child: const Text('הפעל מיקום'),
                    ),
                    TextButton(
                      onPressed: () {
                        Navigator.of(ctx).pop();
                      },
                      child: const Text('ביטול'),
                    ),
                  ],
                ),
          );
        }
      }
    });
  }

  Future<void> _fetchPreferences() async {
    setState(() {
      isLoading = true;
    });
    try {
      // Get user_id from local storage
      final userData = await LocalStorageService.getUserData();
      final userIdStr = userData['user_id'] ?? '';
      final int? userId = int.tryParse(userIdStr.toString());
      if (userId == null) {
        _showError('לא נמצא מזהה משתמש');
        setState(() {
          isLoading = false;
        });
        return;
      }
      final url = Uri.parse(
        'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/GetUserPreferences?user_id=$userId',
      );
      print('DEBUG: getUserPreferences called with user_id = $userId');
      final response = await http.get(url);
      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        final prefs = data['preferences'];
        print('DEBUG: prefs from server: $prefs'); // הוסף שורה זו
        setState(() {
          preferredShelterType = _mapShelterType(prefs['shelterType']);
          accessibilityRequired =
              prefs['accessibilityNeeded'] == true ? 'כן' : 'לא';
          petsAllowed = prefs['petsAllowed'] == true ? 'כן' : 'לא';
          isLoading = false;
        });
      } else {
        final data = json.decode(response.body);
        _showError(data['message'] ?? 'שגיאה בטעינת העדפות');
        setState(() {
          isLoading = false;
        });
      }
    } catch (e) {
      _showError('שגיאה בטעינת העדפות');
      setState(() {
        isLoading = false;
      });
    }
  }

  String _mapShelterType(String? type) {
    if (type == null) return shelterTypes[0];
    if (type == 'PRIVATE' || type == 'פרטי') return 'פרטי';
    if (type == 'PUBLIC' || type == 'ציבורי') return 'ציבורי';
    return shelterTypes[0];
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message, textDirection: TextDirection.rtl),
        backgroundColor: Colors.red,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: BasePage(
        child: Container(
          width: double.infinity,
          height: double.infinity,
          color: const Color(0xFFF7F7F7), // צבע רקע זהה למסך הניווט
          child: Padding(
            padding: const EdgeInsets.only(top: 60.0, left: 20.0, right: 20.0),
            child:
                isLoading
                    ? const Center(child: CircularProgressIndicator())
                    : SingleChildScrollView(
                      child: Column(
                        children: [
                          Row(
                            children: [
                              IconButton(
                                icon: const Icon(Icons.arrow_back_ios),
                                onPressed: () {
                                  Navigator.pushReplacement(
                                    context,
                                    MaterialPageRoute(
                                      builder:
                                          (context) => const SettingsPage(
                                            title: 'הגדרות',
                                          ),
                                    ),
                                  );
                                },
                              ),
                              Expanded(
                                child: Align(
                                  alignment: Alignment.center,
                                  child: Image.asset(
                                    'assets/images/LOGO1.png',
                                    width: 100,
                                    height: 100,
                                  ),
                                ),
                              ),
                              const SizedBox(
                                width: 48,
                              ), // ריווח כמו גודל האייקון
                            ],
                          ),
                          const SizedBox(height: 30),
                          const Text(
                            'העדפות',
                            style: TextStyle(
                              fontSize: 24,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 20),
                          // בשדה סוג מרחב מוגן מועדף, הוסף את ה-key ל-Builder עוטף
                          Builder(
                            key: _dropdownFieldKey,
                            builder:
                                (context) => _buildDropdownField(
                                  'סוג מרחב מוגן',
                                  shelterTypes,
                                  preferredShelterType,
                                  (val) {
                                    if (val != null &&
                                        val != shelterTypes[0] &&
                                        val != preferredShelterType) {
                                      String typeMsg =
                                          val == 'פרטי'
                                              ? 'המערכת לא תנווט אותך למרחבים מוגנים מסוג ציבורי'
                                              : val == 'ציבורי'
                                              ? 'המערכת לא תנווט אותך למרחבים מוגנים מסוג פרטי'
                                              : 'המערכת תנווט אותך רק לסוג מרחב מוגן שבחרת';
                                      final overlay = Overlay.of(context);
                                      WidgetsBinding.instance.addPostFrameCallback((
                                        _,
                                      ) {
                                        final RenderBox? dropdownBox =
                                            _dropdownFieldKey.currentContext
                                                    ?.findRenderObject()
                                                as RenderBox?;
                                        final Offset offset =
                                            dropdownBox != null
                                                ? dropdownBox.localToGlobal(
                                                  Offset.zero,
                                                )
                                                : Offset(0, 0);
                                        OverlayEntry? overlayEntry;
                                        overlayEntry = OverlayEntry(
                                          builder:
                                              (context) => Positioned(
                                                top:
                                                    offset.dy +
                                                    dropdownBox!.size.height +
                                                    10,
                                                left: 20,
                                                right: 20,
                                                child: Material(
                                                  color: Colors.transparent,
                                                  child: Container(
                                                    padding:
                                                        const EdgeInsets.symmetric(
                                                          vertical: 14,
                                                          horizontal: 18,
                                                        ),
                                                    decoration: BoxDecoration(
                                                      color: Colors.blue,
                                                      borderRadius:
                                                          BorderRadius.circular(
                                                            12,
                                                          ),
                                                      boxShadow: [
                                                        BoxShadow(
                                                          color: Colors.black26,
                                                          blurRadius: 8,
                                                          offset: Offset(0, 2),
                                                        ),
                                                      ],
                                                    ),
                                                    child: Row(
                                                      children: [
                                                        Expanded(
                                                          child: Text(
                                                            typeMsg,
                                                            style: const TextStyle(
                                                              color:
                                                                  Colors.white,
                                                              fontSize:
                                                                  14, // מוקטן מ-15 ל-13
                                                              fontWeight:
                                                                  FontWeight
                                                                      .normal, // לא מודגש
                                                            ),
                                                            textDirection:
                                                                TextDirection
                                                                    .rtl,
                                                          ),
                                                        ),
                                                        GestureDetector(
                                                          onTap: () {
                                                            overlayEntry
                                                                ?.remove();
                                                          },
                                                          child: const Icon(
                                                            Icons.close,
                                                            color: Colors.white,
                                                            size: 20,
                                                          ),
                                                        ),
                                                      ],
                                                    ),
                                                  ),
                                                ),
                                              ),
                                        );
                                        overlay.insert(overlayEntry);
                                        Future.delayed(
                                          const Duration(seconds: 3),
                                          () {
                                            overlayEntry?.remove();
                                          },
                                        );
                                      });
                                    }
                                    setState(() {
                                      preferredShelterType = val;
                                    });
                                  },
                                ),
                          ),
                          const SizedBox(height: 10),
                          _buildDropdownField(
                            'דרושה נגישות?',
                            yesNoOptions,
                            accessibilityRequired,
                            (val) {
                              setState(() {
                                accessibilityRequired = val;
                              });
                            },
                          ),
                          const SizedBox(height: 10),
                          _buildDropdownField(
                            'נדרשת כניסת בע"ח?',
                            yesNoOptions,
                            petsAllowed,
                            (val) {
                              setState(() {
                                petsAllowed = val;
                              });
                            },
                          ),
                          const SizedBox(height: 20),
                          ElevatedButton(
                            onPressed: () async {
                              setState(() {
                                isLoading = true;
                              });
                              try {
                                final userData =
                                    await LocalStorageService.getUserData();
                                final userIdStr = userData['user_id'] ?? '';
                                final int? userId = int.tryParse(
                                  userIdStr.toString(),
                                );
                                if (userId == null) {
                                  _showError('לא נמצא מזהה משתמש');
                                  setState(() {
                                    isLoading = false;
                                  });
                                  return;
                                }
                                // Map UI values to API values
                                bool accessibility =
                                    accessibilityRequired == 'כן';
                                bool pets = petsAllowed == 'כן';
                                int peopleCount = 2; // ברירת מחדל קבועה
                                // Build preferences object
                                final prefsBody = {
                                  'preferenceId':
                                      0, // שלח 0 או preferenceId אמיתי אם תוסיף אותו ל-state
                                  'userId': userId,
                                  'shelterType':
                                      preferredShelterType ??
                                      '', // שלח את הערך בעברית כפי שמוצג למשתמש
                                  'accessibilityNeeded': accessibility,
                                  'numDefaultPeople': "1",
                                  'petsAllowed': pets,
                                  'lastUpdate':
                                      DateTime.now()
                                          .toUtc()
                                          .add(const Duration(hours: 3))
                                          .toIso8601String(),
                                };
                                print(
                                  'DEBUG: Sending preferences to server: ' +
                                      jsonEncode(prefsBody),
                                );
                                final url = Uri.parse(
                                  'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/UpdateUserPreferences',
                                );
                                print(
                                  'DEBUG: Sending preferences to server: ' +
                                      jsonEncode(prefsBody),
                                );
                                final response = await http.put(
                                  url,
                                  headers: {'Content-Type': 'application/json'},
                                  body: jsonEncode(prefsBody),
                                );
                                if (response.statusCode == 200) {
                                  ScaffoldMessenger.of(context).showSnackBar(
                                    const SnackBar(
                                      content: Text(
                                        'העדפות נשמרו בהצלחה!',
                                        textDirection: TextDirection.rtl,
                                      ),
                                      backgroundColor: Colors.green,
                                      behavior: SnackBarBehavior.floating,
                                    ),
                                  );
                                } else {
                                  final data = json.decode(response.body);
                                  _showError(
                                    data['message'] ?? 'שגיאה בשמירת העדפות',
                                  );
                                }
                              } catch (e) {
                                _showError('שגיאה בשמירת העדפות');
                              }
                              setState(() {
                                isLoading = false;
                              });
                            },
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color.fromARGB(
                                255,
                                29,
                                46,
                                89,
                              ),
                              foregroundColor: Colors.white,
                              padding: const EdgeInsets.symmetric(
                                horizontal: 40,
                                vertical: 15,
                              ),
                            ),
                            child: const Text(
                              'שמירה',
                              style: TextStyle(fontSize: 16),
                            ),
                          ),
                          const SizedBox(height: 2),
                          GestureDetector(
                            onTap: () async {
                              final confirmed = await showDialog<bool>(
                                context: context,
                                builder:
                                    (context) => AlertDialog(
                                      title: const Center(
                                        child: Text('אישור איפוס'),
                                      ),
                                      content: const Directionality(
                                        textDirection: TextDirection.rtl,
                                        child: Text(
                                          'האם אתה בטוח שברצונך לאפס את ההעדפות לברירת מחדל?',
                                          textAlign: TextAlign.right,
                                        ),
                                      ),
                                      actions: [
                                        TextButton(
                                          onPressed:
                                              () => Navigator.of(
                                                context,
                                              ).pop(false),
                                          child: const Text('ביטול'),
                                        ),
                                        TextButton(
                                          onPressed:
                                              () => Navigator.of(
                                                context,
                                              ).pop(true),
                                          child: const Text('אישור'),
                                        ),
                                      ],
                                    ),
                              );
                              if (confirmed == true) {
                                setState(() {
                                  isLoading = true;
                                });
                                try {
                                  final userData =
                                      await LocalStorageService.getUserData();
                                  final userIdStr = userData['user_id'] ?? '';
                                  final int? userId = int.tryParse(
                                    userIdStr.toString(),
                                  );
                                  if (userId == null) {
                                    _showError('לא נמצא מזהה משתמש');
                                    setState(() {
                                      isLoading = false;
                                    });
                                    return;
                                  }
                                  final url = Uri.parse(
                                    'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/ResetUserPreferences?user_id=$userId',
                                  );
                                  final response = await http.put(url);
                                  if (response.statusCode == 200) {
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      const SnackBar(
                                        content: Text(
                                          'העדפות אופסו לברירת מחדל',
                                          textDirection: TextDirection.rtl,
                                        ),
                                        backgroundColor: Colors.blue,
                                        behavior: SnackBarBehavior.floating,
                                      ),
                                    );
                                    await _fetchPreferences();
                                  } else {
                                    final data = json.decode(response.body);
                                    _showError(
                                      data['message'] ?? 'שגיאה באיפוס העדפות',
                                    );
                                  }
                                } catch (e) {
                                  _showError('שגיאה באיפוס העדפות');
                                }
                                setState(() {
                                  isLoading = false;
                                });
                              }
                            },
                            child: const Text(
                              'איפוס העדפות',
                              style: TextStyle(
                                color: Colors.red,
                                decoration: TextDecoration.underline,
                                decorationColor: Colors.red,
                                decorationThickness: 2,
                                fontSize: 13, // מוקטן מ-16 ל-13
                                fontWeight: FontWeight.bold,
                              ),
                              textAlign: TextAlign.center,
                            ),
                          ),
                        ],
                      ),
                    ),
          ),
        ),
      ),
    );
  }

  Widget _buildDropdownField(
    String label,
    List<String> options,
    String? selectedValue,
    void Function(String?) onChanged,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        SizedBox(
          height: 52, // גובה גדול יותר לשדה דרופדאון
          child: DropdownButtonFormField<String>(
            value: selectedValue ?? options.first,
            isExpanded: true,
            decoration: InputDecoration(
              filled: true,
              fillColor: const Color(0xFFB0C4DE),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: BorderSide.none,
              ),
            ),
            style: const TextStyle(
              fontSize: 12,
              color: Color.fromARGB(255, 29, 46, 89), // כחול כהה לטקסט
            ),
            items:
                options
                    .map(
                      (option) => DropdownMenuItem(
                        value: option,
                        child: Align(
                          alignment: Alignment.centerRight,
                          child: Text(
                            option,
                            textAlign: TextAlign.right,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ),
                    )
                    .toList(),
            onChanged: onChanged,
          ),
        ),
      ],
    );
  }

  @override
  void dispose() {
    _locationCheckTimer?.cancel();
    super.dispose();
  }
}
