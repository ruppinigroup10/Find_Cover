import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'base_page.dart';

// קומפוננטה לעריכת מקלט קיים, מקבלת shelterId
class UpdateMyShelterPage extends StatefulWidget {
  final String? shelterId;
  const UpdateMyShelterPage({super.key, this.shelterId});

  @override
  State<UpdateMyShelterPage> createState() => _UpdateMyShelterPageState();
}

class _UpdateMyShelterPageState extends State<UpdateMyShelterPage> {
  // הגדרת קונטרולרים לשדות הטקסט
  final TextEditingController _addressController = TextEditingController();
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _capacityController = TextEditingController();
  final TextEditingController _additionalInfoController =
      TextEditingController();
  String _isAccessible = 'לא';
  String _allowPets = 'לא';
  bool isLoading = true;
  String? errorMessage;
  List shelters = [];

  String _latitude = '';
  String _longitude = '';
  bool _isActive = true;
  String _createdAt = '';
  String _lastUpdated = '';
  String _shelterType = '';
  String _providerId = '';

  @override
  void initState() {
    super.initState();
    // שליפת נתוני המקלט מהשרת לאחר חצי שנייה
    Future.delayed(const Duration(milliseconds: 500), () {
      _fetchAndFillShelterFromServer();
    });
  }

  // פונקציה שמביאה את נתוני המקלט מהשרת ומשתילה אותם בשדות
  Future<void> _fetchAndFillShelterFromServer() async {
    final shelterId = widget.shelterId;
    print('shelter_id שנשלח לשרת: $shelterId');
    if (shelterId == null || shelterId.isEmpty) {
      setState(() {
        errorMessage = 'לא התקבל מזהה מקלט תקין.';
        isLoading = false;
      });
      return;
    }
    try {
      final response = await http.get(
        Uri.parse(
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/getShelter?shelter_id=$shelterId',
        ),
      );
      print('--- קיבלתי תשובה מהשרת (getShelter) ---');
      print('סטטוס: \\${response.statusCode}');
      print('גוף התשובה: \\${response.body}');
      if (response.statusCode == 200) {
        // פענוח תשובת השרת
        final decoded = jsonDecode(response.body);
        print('decoded:');
        print(decoded);
        // חילוץ אובייקט המקלט מהתשובה
        final shelter =
            decoded['shlter'] ??
            decoded['shelter'] ??
            decoded['Shelter'] ??
            decoded['ShelterData'];
        print('shelter map:');
        print(shelter);
        if (shelter != null && shelter is Map) {
          // הדפסת כל הערכים שהתקבלו מהשרת
          print('--- כל הערכים שהתקבלו מהשרת עבור מקלט זה ---');
          shelter.forEach((key, value) {
            print('  $key: $value');
          });
          // ראשית, עדכון ערכי הקונטרולרים והמשתנים
          _addressController.text = (shelter['address'] ?? '').toString();
          _nameController.text = (shelter['name'] ?? '').toString();
          _capacityController.text = (shelter['capacity'] ?? '').toString();
          _additionalInfoController.text =
              (shelter['additional_information'] ?? '').toString();
          _isAccessible =
              (shelter['is_accessible'] == true ||
                      shelter['is_accessible'] == 'true' ||
                      shelter['is_accessible'] == 1)
                  ? 'כן'
                  : 'לא';
          _allowPets =
              (shelter['pets_friendly'] == true ||
                      shelter['pets_friendly'] == 'true' ||
                      shelter['pets_friendly'] == 1)
                  ? 'כן'
                  : 'לא';
          _latitude = (shelter['latitude'] ?? '').toString();
          _longitude = (shelter['longitude'] ?? '').toString();
          _isActive =
              (shelter['is_active'] == true ||
                  shelter['is_active'] == 'true' ||
                  shelter['is_active'] == 1);
          _createdAt = (shelter['created_at'] ?? '').toString();
          _lastUpdated = (shelter['last_updated'] ?? '').toString();
          _shelterType = (shelter['shelter_type'] ?? '').toString();
          _providerId = (shelter['provider_id'] ?? '').toString();
          isLoading = false;
          errorMessage = null;
          setState(() {}); // רענון UI לאחר עדכון ערכים
          // הדפסה אחרי שתילה
          print('--- אחרי עדכון ערכים ---');
          print('addressController: ' + _addressController.text);
          print('nameController: ' + _nameController.text);
          print('capacityController: ' + _capacityController.text);
          print('additionalInfoController: ' + _additionalInfoController.text);
          print('_isAccessible: ' + _isAccessible);
          print('_allowPets: ' + _allowPets);
          print('_latitude: ' + _latitude);
          print('_longitude: ' + _longitude);
          print('_isActive: ' + _isActive.toString());
          print('_createdAt: ' + _createdAt);
          print('_lastUpdated: ' + _lastUpdated);
          print('_shelterType: ' + _shelterType);
          print('_providerId: ' + _providerId);
          // בדיקה לאחר בניית UI
          WidgetsBinding.instance.addPostFrameCallback((_) {
            print('--- Post-frame: controller values after build ---');
            print('addressController: ' + _addressController.text);
            print('nameController: ' + _nameController.text);
            print('capacityController: ' + _capacityController.text);
            print(
              'additionalInfoController: ' + _additionalInfoController.text,
            );
          });
        } else {
          // טיפול במקרה שלא התקבלו נתונים תקינים
          setState(() {
            errorMessage = 'לא התקבלו נתונים תקינים מהשרת.';
            isLoading = false;
          });
        }
        print('--- סוף הדפסת getShelter ---');
      } else {
        // טיפול בשגיאה בקבלת נתונים מהשרת
        setState(() {
          errorMessage =
              'שגיאה בטעינת נתוני המקלט (קוד: ︠{response.statusCode})';
          isLoading = false;
        });
      }
    } catch (e) {
      // טיפול בשגיאת תקשורת
      print('שגיאה בשליפת נתוני המקלט מהשרת: $e');
      setState(() {
        errorMessage = 'שגיאה בחיבור לשרת: $e';
        isLoading = false;
      });
    }
  }

  // פונקציה לעדכון נתוני המקלט בשרת
  Future<void> _updateShelter() async {
    final providerId =
        (await SharedPreferences.getInstance()).getString('user_id') ?? '';
    final shelterIdStr = widget.shelterId ?? '';
    if (shelterIdStr.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('שגיאה: לא נמצא מזהה מקלט לעדכון.')),
      );
      return;
    }
    final shelterIdInt = int.tryParse(shelterIdStr);
    if (shelterIdInt == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('שגיאה: מזהה מקלט לא תקין (לא מספרי).')),
      );
      return;
    }
    print('shelter_id שנשלח לעדכון: $shelterIdInt');
    final shelterData = {
      // בניית אובייקט המקלט לעדכון
      'ShelterId': shelterIdInt,
      'ProviderId': int.tryParse(providerId) ?? providerId,
      'ShelterType': _shelterType.isNotEmpty ? _shelterType : 'פרטי',
      'Name': _nameController.text.trim(),
      'Latitude': double.tryParse(_latitude) ?? 31.2518,
      'Longitude': double.tryParse(_longitude) ?? 34.7913,
      'Address': _addressController.text.trim(),
      'Capacity': int.tryParse(_capacityController.text.trim()) ?? 0,
      'IsAccessible': _isAccessible == 'כן',
      'PetsFriendly': _allowPets == 'כן',
      'IsActive': _isActive,
      'AdditionalInformation': _additionalInfoController.text.trim(),
    };
    // הסרתי הדפסות כפולות - רק הדפסה אחת של כל הערכים שנשלחים לשרת
    print('--- כל הערכים שנשלחים לשרת בעת עדכון ---');
    print('shelterId (לעדכון): $shelterIdInt');
    shelterData.forEach((k, v) => print('  $k: $v'));
    print('-------------------------------');
    try {
      final response = await http.put(
        Uri.parse(
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/UpdateShelter',
        ),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(shelterData),
      );
      print('סטטוס קוד: [32m${response.statusCode}[0m');
      print('תשובת שרת: ${response.body}');
      if (response.statusCode == 200) {
        // טיפול בהצלחה - עדכון לוקאלי ורענון
        // --- עדכון נתוני המקלט בלוקל סטורג' לאחר עדכון (כמו בעמוד אזור מוכר) ---
        try {
          final shelterResponse = await http.get(
            Uri.parse(
              'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/getShelter?shelter_id=$shelterIdInt',
            ),
          );
          if (shelterResponse.statusCode == 200) {
            final decoded = jsonDecode(shelterResponse.body);
            final shelter = decoded['shlter'];
            if (shelter != null) {
              final prefs = await SharedPreferences.getInstance();
              await prefs.setString('my_shelter', jsonEncode(shelter));
              print('נתוני המקלט נשמרו בלוקל סטורג\'');
            }
          }
        } catch (e) {
          print('שגיאה בעדכון נתוני המקלט בלוקל סטורג\': $e');
        }
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('המקלט עודכן בהצלחה!'),
            backgroundColor: Colors.green,
          ),
        );
        // --- רענון נתוני המקלט מהשרת לאחר עדכון ---
        await _fetchAndFillShelterFromServer();
        // ניתן להוסיף כאן לוגיקה נוספת אם רוצים להישאר בדף לאחר עדכון
        // Navigator.pop(context, true); // אם רוצים לחזור אחורה אחרי עדכון
      } else {
        // טיפול בשגיאה בעדכון
        print('שגיאה בעדכון המקלט: ${response.body}');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('שגיאה בעדכון המקלט: ${response.body}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } catch (e) {
      // טיפול בשגיאת תקשורת
      print('שגיאה בחיבור לשרת: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('שגיאה בחיבור לשרת.'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  // כפתור רענון ידני לטעינת נתונים מחדש
  Widget _buildRefreshButton() {
    return IconButton(
      icon: const Icon(Icons.refresh, color: Color(0xFF1D2E59)),
      onPressed: () async {
        setState(() => isLoading = true);
        await _fetchAndFillShelterFromServer();
      },
      tooltip: 'רענן נתונים',
    );
  }

  @override
  Widget build(BuildContext context) {
    // בניית ממשק המשתמש של עמוד עריכת מקלט
    return Directionality(
      textDirection: TextDirection.rtl,
      child: BasePage(
        child: Scaffold(
          backgroundColor: const Color(0xFFF8F5FB),
          appBar: AppBar(
            // סרגל עליון עם כפתור חזור ולוגו
            backgroundColor: Colors.transparent,
            elevation: 0,
            toolbarHeight: 200, // גובה 100 פיקסל
            leading: IconButton(
              icon: const Icon(Icons.arrow_back_ios, color: Color(0xFF1D2E59)),
              onPressed: () => Navigator.pop(context),
            ),
            title: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Image.asset('assets/images/LOGO1.png', width: 100, height: 60),
                const SizedBox(height: 2),
                const Text(
                  'עריכת מרחב מוגן',
                  style: TextStyle(
                    color: Color(0xFF1D2E59),
                    fontWeight: FontWeight.bold,
                    fontSize: 24,
                  ),
                ),
              ],
            ),
            centerTitle: true,
            actions: [_buildRefreshButton()],
          ),
          body:
              isLoading
                  ? const Center(
                    child: CircularProgressIndicator(),
                  ) // טוען נתונים
                  : errorMessage != null
                  ? Column(
                    // הצגת הודעת שגיאה
                    mainAxisAlignment: MainAxisAlignment.start,
                    children: [
                      const SizedBox(height: 30),
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 24.0),
                        child: Text(
                          errorMessage!,
                          style: const TextStyle(
                            color: Colors.red,
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                          textAlign: TextAlign.center,
                        ),
                      ),
                    ],
                  )
                  : SingleChildScrollView(
                    // טופס עריכת מקלט
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 24.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: [
                          const SizedBox(height: 0),
                          _buildTextField(
                            'הזן כתובת',
                            'עיר, רחוב, מספר בית',
                            _addressController,
                          ),
                          const SizedBox(height: 16),
                          _buildTextField('שם', 'הבית של דני', _nameController),
                          const SizedBox(height: 16),
                          _buildTextField(
                            'תפוסה מקסימלית',
                            '7',
                            _capacityController,
                            keyboardType: TextInputType.number,
                          ),
                          const SizedBox(height: 16),
                          _buildDropdownField(
                            'לאפשר כניסת חיות?',
                            ['לא', 'כן'],
                            (value) {
                              setState(() {
                                _allowPets = value!;
                              });
                            },
                            _allowPets,
                          ),
                          const SizedBox(height: 16),
                          _buildDropdownField('קיימת נגישות?', ['לא', 'כן'], (
                            value,
                          ) {
                            setState(() {
                              _isAccessible = value!;
                            });
                          }, _isAccessible),
                          const SizedBox(height: 16),
                          _buildTextField(
                            'הערות?',
                            '',
                            _additionalInfoController,
                            maxLength: 255,
                          ),
                          const SizedBox(height: 16),
                          SizedBox(
                            // הסר את width: double.infinity כדי לאפשר גודל דינאמי
                            child: ElevatedButton(
                              onPressed: _updateShelter,
                              style: ElevatedButton.styleFrom(
                                backgroundColor: const Color(0xFF1D2E59),
                                foregroundColor: Colors.white,
                                padding: const EdgeInsets.symmetric(
                                  vertical: 14,
                                  horizontal: 24, // הוסף padding אופקי יפה
                                ),
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(12),
                                ),
                              ),
                              child: const Text(
                                'עדכן מקלט',
                                style: TextStyle(fontSize: 14),
                              ),
                            ),
                          ),
                          const SizedBox(height: 5),
                          // כפתור מחיקת מקלט אדום עם קו תחתון אדום
                          GestureDetector(
                            onTap: () async {
                              final confirmed = await showDialog<bool>(
                                context: context,
                                builder:
                                    (context) => AlertDialog(
                                      title: const Center(
                                        child: Text(
                                          'אישור מחיקה',
                                          textAlign: TextAlign.center,
                                        ),
                                      ),
                                      content: const Directionality(
                                        textDirection: TextDirection.rtl,
                                        child: Text(
                                          'האם אתה בטוח שברצונך למחוק את המקלט?',
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
                                final prefs =
                                    await SharedPreferences.getInstance();
                                final providerId =
                                    prefs.getString('user_id') ?? '';
                                final shelterId = widget.shelterId;
                                final url = Uri.parse(
                                  'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/DeleteShelter?shelter_id=$shelterId&provider_id=$providerId',
                                );
                                try {
                                  final response = await http.delete(url);
                                  if (response.statusCode == 200) {
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      const SnackBar(
                                        content: Text('המקלט נמחק בהצלחה!'),
                                        backgroundColor: Colors.red,
                                      ),
                                    );
                                    Navigator.pop(context, true);
                                  } else {
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      SnackBar(
                                        content: Text(
                                          'שגיאה במחיקת המקלט: {response.body}',
                                        ),
                                        backgroundColor: Colors.red,
                                      ),
                                    );
                                  }
                                } catch (e) {
                                  ScaffoldMessenger.of(context).showSnackBar(
                                    const SnackBar(
                                      content: Text('שגיאת חיבור לשרת'),
                                      backgroundColor: Colors.red,
                                    ),
                                  );
                                }
                              }
                            },
                            child: Container(
                              width: double.infinity,
                              padding: const EdgeInsets.symmetric(vertical: 2),
                              child: const Text(
                                'מחק מקלט',
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  color: Colors.red,
                                  fontSize: 13,
                                  fontWeight: FontWeight.bold,
                                  decoration: TextDecoration.underline,
                                  decorationColor: Colors.red,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(height: 16),
                        ],
                      ),
                    ),
                  ),
        ),
      ),
    );
  }

  // בניית שדה טקסט עם תווית
  Widget _buildTextField(
    String label,
    String hint,
    TextEditingController controller, {
    int? maxLength,
    TextInputType? keyboardType,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        TextField(
          controller: controller,
          maxLength: maxLength,
          keyboardType: keyboardType,
          style: const TextStyle(fontSize: 15, color: Color(0xFF1D2E59)),
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: const TextStyle(fontSize: 14, color: Color(0xFF7B8FA1)),
            filled: true,
            fillColor: const Color(0xFFB0C4DE),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide.none,
            ),
            counterText: '',
          ),
        ),
      ],
    );
  }

  // בניית שדה בחירה (Dropdown) עם תווית
  Widget _buildDropdownField(
    String label,
    List<String> options,
    ValueChanged<String?> onChanged,
    String selectedValue,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        DropdownButtonFormField<String>(
          value: selectedValue,
          decoration: InputDecoration(
            filled: true,
            fillColor: const Color(0xFFB0C4DE),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide.none,
            ),
          ),
          style: const TextStyle(fontSize: 15, color: Color(0xFF1D2E59)),
          items:
              options
                  .map(
                    (option) =>
                        DropdownMenuItem(value: option, child: Text(option)),
                  )
                  .toList(),
          onChanged: onChanged,
        ),
      ],
    );
  }

  // בניית שדה לקריאה בלבד (read-only)
  Widget _buildReadOnlyField(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 10),
          decoration: BoxDecoration(
            color: Color(0xFFB0C4DE),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Text(
            value,
            style: const TextStyle(fontSize: 15, color: Color(0xFF1D2E59)),
          ),
        ),
      ],
    );
  }
}
