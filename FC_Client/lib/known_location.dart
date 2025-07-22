import 'package:flutter/material.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'base_page.dart'; // ייבוא BasePage
import 'settings.dart'; // ייבוא עמוד ההגדרות
import 'local_storage_service.dart';
import 'add_known_location.dart';
import 'update_known_location.dart';

class KnownLocationPage extends StatefulWidget {
  const KnownLocationPage({super.key});

  @override
  State<KnownLocationPage> createState() => _KnownLocationPageState();
}

class _KnownLocationPageState extends State<KnownLocationPage> {
  List<Map<String, dynamic>> knownLocationsList = [];
  List<String> userLocations = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    Future.delayed(const Duration(milliseconds: 500), () {
      _fetchUserLocations();
    });
  }

  Future<void> _fetchUserLocations() async {
    final userData = await LocalStorageService.getUserData();
    final userId = userData['user_id'];
    // קריאה אמיתית לשרת לקבלת אזורים מוכרים
    try {
      final url = Uri.parse(
        'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/GetMyKnownLocations?user_id=$userId',
      );
      final response = await http.get(url);
      if (response.statusCode == 200) {
        final responseData = jsonDecode(response.body);
        final knownLocations = responseData['knownLocations'];
        print('--- knownLocations מהשרת ---');
        if (knownLocations != null && knownLocations is List) {
          for (final loc in knownLocations) {
            print(loc.toString());
          }
        }
        print('--- סוף הדפסת knownLocations ---');
        if (knownLocations != null &&
            knownLocations is List &&
            knownLocations.isNotEmpty) {
          setState(() {
            knownLocationsList = List<Map<String, dynamic>>.from(
              knownLocations.map((loc) => Map<String, dynamic>.from(loc)),
            );
            userLocations = List<String>.from(
              knownLocations.map(
                (loc) => loc['locationName']?.toString() ?? '',
              ),
            );
            _isLoading = false;
          });
        } else {
          setState(() {
            userLocations = [];
            _isLoading = false;
          });
        }
      } else {
        setState(() {
          userLocations = [];
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        userLocations = [];
        _isLoading = false;
      });
    }
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
            padding: const EdgeInsets.symmetric(horizontal: 20.0),
            child: Column(
              children: [
                const SizedBox(height: 30),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    IconButton(
                      icon: const Icon(Icons.arrow_back_ios),
                      onPressed: () {
                        Navigator.pushReplacement(
                          context,
                          MaterialPageRoute(
                            builder:
                                (context) =>
                                    const SettingsPage(title: 'הגדרות'),
                          ),
                        );
                      },
                    ),
                    Image.asset(
                      'assets/images/LOGO1.png',
                      width: 100,
                      height: 100,
                    ),
                    const SizedBox(width: 48),
                  ],
                ),
                const SizedBox(height: 10),
                const Text(
                  'אזורים מוכרים',
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 20),
                if (_isLoading)
                  const Expanded(
                    child: Center(child: CircularProgressIndicator()),
                  )
                else if (userLocations.isEmpty)
                  Expanded(
                    child: Column(
                      children: [
                        const Spacer(),
                        const Center(child: Text('אין אזורים מוכרים')),
                        const SizedBox(height: 25),
                        Center(
                          child: ElevatedButton(
                            onPressed: () async {
                              await Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder:
                                      (context) => const AddKnownLocationPage(),
                                ),
                              );
                              _fetchUserLocations();
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
                              'להוספת אזור מוכר',
                              style: TextStyle(fontSize: 16),
                            ),
                          ),
                        ),
                        const Spacer(),
                      ],
                    ),
                  )
                else
                  Expanded(
                    child: ListView.builder(
                      itemCount:
                          userLocations.length +
                          1, // כפתור ההוספה הוא פריט אחרון
                      padding: const EdgeInsets.only(bottom: 30),
                      itemBuilder: (context, index) {
                        if (index < userLocations.length) {
                          final location = userLocations[index];
                          final locationData = knownLocationsList[index];
                          return Padding(
                            padding: const EdgeInsets.symmetric(vertical: 8.0),
                            child: _buildLocationButton(
                              context,
                              location,
                              locationData,
                            ),
                          );
                        } else {
                          // כפתור ההוספה - תמיד 50 פיקסלים אחרי האחרון
                          return Padding(
                            padding: const EdgeInsets.only(top: 50, bottom: 10),
                            child: Center(
                              child: ElevatedButton(
                                onPressed: () async {
                                  await Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                      builder:
                                          (context) =>
                                              const AddKnownLocationPage(),
                                    ),
                                  );
                                  _fetchUserLocations();
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
                                  'להוספת אזור מוכר',
                                  style: TextStyle(fontSize: 16),
                                ),
                              ),
                            ),
                          );
                        }
                      },
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildLocationButton(
    BuildContext context,
    String title,
    Map<String, dynamic> locationData,
  ) {
    return ElevatedButton(
      onPressed: () async {
        await Navigator.push(
          context,
          MaterialPageRoute(
            builder:
                (context) =>
                    UpdateKnownLocationPage(locationData: locationData),
          ),
        );
        _fetchUserLocations(); // רענון הרשימה מיד לאחר חזרה מעדכון
      },
      style: ElevatedButton.styleFrom(
        backgroundColor: const Color(0xFFB0C4DE),
        foregroundColor: Colors.black,
        padding: const EdgeInsets.symmetric(vertical: 15),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(title, style: const TextStyle(fontSize: 16)),
          const Icon(Icons.arrow_forward_ios, size: 16),
        ],
      ),
    );
  }
}
