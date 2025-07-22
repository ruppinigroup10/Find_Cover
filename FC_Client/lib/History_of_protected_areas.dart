import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'base_page.dart'; // ייבוא BasePage
import 'settings.dart'; // ייבוא עמוד ההגדרות
import 'local_storage_service.dart'; // לקבלת מזהה המשתמש

class HistoryOfProtectedAreasPage extends StatefulWidget {
  const HistoryOfProtectedAreasPage({super.key});

  @override
  State<HistoryOfProtectedAreasPage> createState() =>
      _HistoryOfProtectedAreasPageState();
}

class _HistoryOfProtectedAreasPageState
    extends State<HistoryOfProtectedAreasPage> {
  List<dynamic> _visitHistory = [];
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadUserVisitHistory();
  }

  Future<void> _loadUserVisitHistory() async {
    try {
      // קבלת מזהה המשתמש מה-local storage
      final userData = await LocalStorageService.getUserData();
      final userId = userData['user_id'];

      if (userId == null || userId.toString().isEmpty) {
        setState(() {
          _isLoading = false;
          _errorMessage = 'לא נמצא מזהה משתמש. אנא התחבר מחדש.';
        });
        return;
      }
      print('Loading visit history for user: $userId');

      // שליחת בקשה לשרת
      final response = await http
          .get(
            Uri.parse(
              'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/GetUserVisitHistory?user_id=$userId',
            ),
            headers: {'Content-Type': 'application/json'},
          )
          .timeout(const Duration(seconds: 10));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        setState(() {
          _visitHistory = data['visits'] ?? [];
          _isLoading = false;
          _errorMessage = null;
        });
        print(
          'Visit history loaded successfully: ${_visitHistory.length} visits',
        );
      } else {
        final errorData = json.decode(response.body);
        setState(() {
          _isLoading = false;
          _errorMessage = errorData['message'] ?? 'שגיאה בטעינת ההיסטוריה';
        });
        print('Error loading visit history: ${response.statusCode}');
      }
    } catch (e) {
      setState(() {
        _isLoading = false;
        _errorMessage = 'שגיאה בחיבור לשרת: $e';
      });
      print('Exception loading visit history: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Container(
          width: double.infinity,
          height: double.infinity,
          color: const Color(0xFFF7F7F7), // צבע רקע זהה למסך הניווט
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20.0),
            child: Column(
              children: [
                const SizedBox(height: 20),
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
                      'assets/images/LOGO1.png', // עדכן את הנתיב לתמונה שלך
                      width: 100,
                      height: 100,
                    ),
                    const SizedBox(width: 48), // ריווח כדי לאזן את הלוגו
                  ],
                ),
                const SizedBox(height: 10),
                const Text(
                  'היסטוריית מרחבים מוגנים',
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 20),
                Expanded(
                  child:
                      _isLoading
                          ? const Center(
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                CircularProgressIndicator(),
                                SizedBox(height: 16),
                                Text('טוען היסטוריית ביקורים...'),
                              ],
                            ),
                          )
                          : _errorMessage != null
                          ? Center(
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                const Icon(
                                  Icons.error_outline,
                                  size: 64,
                                  color: Colors.red,
                                ),
                                const SizedBox(height: 16),
                                Text(
                                  _errorMessage!,
                                  style: const TextStyle(
                                    fontSize: 16,
                                    color: Colors.red,
                                  ),
                                  textAlign: TextAlign.center,
                                ),
                                const SizedBox(height: 16),
                                ElevatedButton(
                                  onPressed: () {
                                    setState(() {
                                      _isLoading = true;
                                      _errorMessage = null;
                                    });
                                    _loadUserVisitHistory();
                                  },
                                  child: const Text('נסה שוב'),
                                ),
                              ],
                            ),
                          )
                          : _visitHistory.isEmpty
                          ? const Center(
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(
                                  Icons.history,
                                  size: 64,
                                  color: Colors.grey,
                                ),
                                SizedBox(height: 16),
                                Text(
                                  'אין היסטוריית ביקורים',
                                  style: TextStyle(
                                    fontSize: 18,
                                    color: Colors.grey,
                                  ),
                                ),
                                SizedBox(height: 8),
                                Text(
                                  'כאשר תבקר במרחבים מוגנים, הם יופיעו כאן',
                                  style: TextStyle(
                                    fontSize: 14,
                                    color: Colors.grey,
                                  ),
                                  textAlign: TextAlign.center,
                                ),
                              ],
                            ),
                          )
                          : ListView.builder(
                            itemCount: _visitHistory.length,
                            itemBuilder: (context, index) {
                              final visit = _visitHistory[index];
                              return _buildHistoryCard(
                                date: visit['arrivalTime'] ?? 'תאריך לא זמין',
                                area: visit['shelterAddress'] ?? 'אזור לא זמין',
                                shelter: visit['shelterName'] ?? 'מרחב לא זמין',
                                visitInfo: visit,
                              );
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

  Widget _buildHistoryCard({
    required String date,
    required String area,
    required String shelter,
    Map<String, dynamic>? visitInfo,
  }) {
    return Card(
      color: const Color(0xFFB0C4DE),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.all(15.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'תאריך הגעה: $date',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 5),
            Text('כתובת המקלט: $area', style: const TextStyle(fontSize: 14)),
            const SizedBox(height: 5),
            Text('שם המקלט: $shelter', style: const TextStyle(fontSize: 14)),
            // הצגת מידע נוסף מהשרת
            if (visitInfo != null) ...[
              if (visitInfo['visitId'] != null) ...[
                const SizedBox(height: 5),
                Text(
                  'מזהה ביקור: ${visitInfo['visitId']}',
                  style: const TextStyle(fontSize: 12, color: Colors.black54),
                ),
              ],
              if (visitInfo['additionalInformation'] != null &&
                  visitInfo['additionalInformation'].toString().isNotEmpty) ...[
                const SizedBox(height: 3),
                Text(
                  'מידע נוסף: ${visitInfo['additionalInformation']}',
                  style: const TextStyle(fontSize: 12, color: Colors.black54),
                ),
              ],
            ],
          ],
        ),
      ),
    );
  }
}
