@tool
extends Node3D

# Nút bấm để tạo sân
@export var Tao_San: bool = false:
	set(val): if val: bat_dau_xay()

# Nút dọn dẹp (nếu muốn làm lại)
@export var Xoa_Het: bool = false:
	set(val): if val: don_dep()

func don_dep():
	# Xóa tất cả, CHỈ GIỮ LẠI Tile_1_1 làm mẫu
	for con in get_children():
		if con.name != "Tile_1_1":
			con.queue_free()
	print("🧹 Đã dọn sạch bàn cờ!")

func bat_dau_xay():
	var tile_mau = get_node_or_null("Tile_1_1")
	if not tile_mau:
		print("❌ Lỗi: Không tìm thấy Tile_1_1 để làm mẫu!")
		return

	print("🏗️ Đang xây bàn cờ 8x8 (Sao chép toàn bộ nhánh)...")
	
	# Lấy chủ Scene (Quan trọng để hiện các node con trong Editor)
	var scene_root = get_tree().edited_scene_root
	
	# Vòng lặp 8x8 (X và Z)
	for x in range(1, 9):
		for z in range(1, 9):
			# Bỏ qua ô mẫu (1, 1) vì nó có sẵn rồi
			if x == 1 and z == 1: continue
			
			# Kiểm tra nếu ô đã có thì bỏ qua
			var ten_o = "Tile_%d_%d" % [x, z]
			if has_node(ten_o): continue
			
			# 1. Nhân bản (Duplicate)
			var tile_moi = tile_mau.duplicate(7) # Số 7 = Copy cả Script, Groups, Signals
			tile_moi.name = ten_o
			add_child(tile_moi)
			
			# 2. Đặt vị trí (Mỗi ô cách nhau 2 mét)
			tile_moi.position = Vector3((x - 1) * 2.0, 0, (z - 1) * 2.0)
			
			# 3. [QUAN TRỌNG NHẤT] Gán quyền sở hữu cho TOÀN BỘ node con bên trong
			# Để bạn thấy được mũi tên > và chỉnh sửa được bên trong
			gan_quyen_so_huu_de_quy(tile_moi, scene_root)
			
			# 4. Tô màu xen kẽ (Caro) cho đẹp
			to_mau_o(tile_moi, x, z)

	print("✅ Đã xây xong 64 ô! Hãy bấm Ctrl+S để lưu lại.")

# Hàm đệ quy: Đi sâu vào từng ngóc ngách để báo cáo với Godot Editor
func gan_quyen_so_huu_de_quy(node, root):
	if node != root:
		node.owner = root
	
	# Gọi tiếp cho các con của nó
	for con in node.get_children():
		gan_quyen_so_huu_de_quy(con, root)

# Hàm tô màu bàn cờ vua (Trắng/Đen)
func to_mau_o(tile, x, z):
	# Tìm cái Mesh (hình khối) bên trong
	var mesh = tile.get_node_or_null("MeshInstance3D")
	# Nếu chính Tile là Mesh thì lấy luôn
	if not mesh and tile is MeshInstance3D: mesh = tile
	
	if mesh:
		var mat = StandardMaterial3D.new()
		# Logic bàn cờ vua: Nếu tổng (x+z) là số chẵn -> Màu tối
		if (x + z) % 2 == 0:
			mat.albedo_color = Color(0.2, 0.2, 0.251, 1.0) # Màu Xanh Đen đậm
		else:
			mat.albedo_color = Color(0.5, 0.5, 0.6) # Màu Xám Xanh sáng
		
		mesh.material_override = mat
