#include "BlackBoard.h"

void BlackBoard::SetValue(const std::string& key, const BBValue& value)
{
	m_Data[key] = value;
}

bool BlackBoard::HasKey(const std::string& key) const
{
	return m_Data.find(key) != m_Data.end();
}
